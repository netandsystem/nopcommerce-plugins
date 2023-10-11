using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Data;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.DataStructures;
using Nop.Plugin.Api.Infrastructure;
using System.Threading.Tasks;
using Nop.Plugin.Api.DTO.ShoppingCarts;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Core.Domain.Customers;
using Nop.Services.Security;
using Nop.Services.Orders;
using System.Linq.Dynamic.Core;
using Nop.Services.Localization;
using Nop.Plugin.Api.DTOs.ShoppingCarts;
using MailKit;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using LinqToDB.Common;

namespace Nop.Plugin.Api.Services;

#nullable enable

public class ShoppingCartItemApiService : IShoppingCartItemApiService
{
    #region Fields

    private readonly IRepository<ShoppingCartItem> _shoppingCartItemsRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IStoreContext _storeContext;
    private readonly IProductApiService _productApiService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IPermissionService _permissionService;
    private readonly IRepository<ShoppingCartItem> _sciRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IProductService _productService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
    private readonly ICustomerService _customerService;

    #endregion

    #region Ctro

    public ShoppingCartItemApiService(IRepository<ShoppingCartItem> shoppingCartItemsRepository, IRepository<Product> productRepository, IStoreContext storeContext, IProductApiService productApiService, IShoppingCartService shoppingCartService, IPermissionService permissionService, IRepository<ShoppingCartItem> sciRepository,
      ILocalizationService localizationService,
      IProductService productService,
      ShoppingCartSettings shoppingCartSettings,
      IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
      ICustomerService customerService
    )
    {
        _shoppingCartItemsRepository = shoppingCartItemsRepository;
        _productRepository = productRepository;
        _storeContext = storeContext;
        _productApiService = productApiService;
        _shoppingCartService = shoppingCartService;
        _permissionService = permissionService;
        _sciRepository = sciRepository;
        _localizationService = localizationService;
        _productService = productService;
        _shoppingCartSettings = shoppingCartSettings;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _customerService = customerService;
    }

    #endregion

    #region Methods

    public async Task<List<ShoppingCartItem>> GetShoppingCartItemsAsync(
        int? customerId = null, DateTime? createdAtMin = null, DateTime? createdAtMax = null,
        DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, int? limit = null,
        int? page = null, ShoppingCartType? shoppingCartType = null)
    {
        var query = GetShoppingCartItemsQuery(customerId, createdAtMin, createdAtMax,
                                              updatedAtMin, updatedAtMax, shoppingCartType);

        return await query.ToListAsync();
    }

    public async Task<List<ShoppingCartItemDto>> JoinShoppingCartItemsWithProductsAsync(IList<ShoppingCartItem> shoppingCartItems)
    {
        // get productsDto list
        var productIds = shoppingCartItems.Select(x => x.ProductId).ToList();

        var productQuery = from product in _productRepository.Table
                    where productIds.Contains(product.Id) 
                    select product;

        IList<Product> products = await productQuery.ToListAsync();

        IList<ProductDto> productsDto = await _productApiService.JoinProductsAndPicturesAsync(products);

        // join productsDto and ShoppingCartItemDto

        var shoppingCartItemsQuery = from item in shoppingCartItems
                                   join productDto in productsDto
                                   on item.ProductId equals productDto.Id
                                   select item.ToDto(productDto);

        return await shoppingCartItemsQuery.ToListAsync();
    }

    public Task<ShoppingCartItem> GetShoppingCartItemAsync(int id)
    {
        return _shoppingCartItemsRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Add a productList to shopping cart
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="shoppingCartItems">shopping cart items</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the warnings
    /// </returns>
    public async Task<List<string>> AddProductListToCartAsync(
        Customer customer,
        List<ShoppingCartItemPost> newItems,
        int storeId
    )
    {
        var cart = await GetShoppingCartItemsAsync(customerId: customer.Id);

        //Remove all products with 0 quantity
        newItems.RemoveAll(item => item.Quantity == 0);

        //Validate positive quantities
        foreach (var item in newItems)
        {
            if (item.Quantity <= 0)
            {
                List<string> local_warnings = new()
                {
                    $"item {item.ProductId} quantity should be positive"
                };
                return local_warnings;
            }
        }

        //if there are some products in cart, sum quantities
        foreach (var newItem in newItems) 
        { 
            var oldItem = cart.Find(item => item.ProductId == newItem.ProductId && item.ShoppingCartType == newItem.ShoppingCartType);

            if (oldItem != null)
            {
                int newQuantity = oldItem.Quantity + newItem.Quantity;
                newItem.Quantity = newQuantity;
                oldItem.Quantity = newQuantity;
            }
        }

        List<string> warnings = await GetShoppingCartItemWarningsAsync(customer, newItems);

        if (warnings.Any())
        {
            return warnings;
        }

        List<ShoppingCartItem> shoppingCartItemInsertList = new();
        List<ShoppingCartItem> shoppingCartItemUpdateList = new();

        foreach (var newItem in newItems)
        {
            var oldItem = cart.Find(item => item.ProductId == newItem.ProductId && item.ShoppingCartType == newItem.ShoppingCartType);

            //If is old just update
            if (oldItem != null)
            {
                shoppingCartItemUpdateList.Add(oldItem);
            } 
            // if is new add to cart
            else
            {
                //New shopping cart item
                var now = DateTime.UtcNow;

                var shoppingCartItem = new ShoppingCartItem
                {
                    ShoppingCartType = newItem.ShoppingCartType,
                    StoreId = storeId,
                    ProductId = newItem.ProductId,
                    Quantity = newItem.Quantity,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now,
                    CustomerId = customer.Id
                };

                shoppingCartItemInsertList.Add(shoppingCartItem);
            }
        }

        if (shoppingCartItemInsertList.Any())
        {
            await _sciRepository.InsertAsync(shoppingCartItemInsertList);
        }

        if (shoppingCartItemUpdateList.Any())
        {
            await _sciRepository.UpdateAsync(shoppingCartItemUpdateList);
        }

        //updated "HasShoppingCartItems" property used for performance optimization
        customer.HasShoppingCartItems = shoppingCartItemInsertList.Any() || shoppingCartItemUpdateList.Any();

        await _customerService.UpdateCustomerAsync(customer);

        //No warnigns
        return warnings;
    }

    public async Task AddShoppingCartItemsToCartAsync(
        List<ShoppingCartItem> cart
    )
    {
        await _sciRepository.InsertAsync(cart);
    }

    /// <summary>
    /// Add a productList to shopping cart and delete the previous items
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="shoppingCartItems">shopping cart items</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the warnings
    /// </returns>
    public async Task<List<string>> ReplaceCartAsync(
        Customer customer, 
        List<ShoppingCartItemPost> newItems,
        int storeId
    )
    {
        //Remove all products with 0 quantity
        newItems.RemoveAll(item => item.Quantity == 0);

        List<string> warnings = await GetShoppingCartItemWarningsAsync(customer, newItems);

        if (warnings.Any())
        {
            return warnings;
        }

        var shoppingCartItems = newItems.Where(items => items.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
        var whishListItems = newItems.Where(items => items.ShoppingCartType == ShoppingCartType.Wishlist).ToList();

        var carts = new List<List<ShoppingCartItemPost>>()
        {
            shoppingCartItems,
            whishListItems
        };

        var types = new List<ShoppingCartType>() { ShoppingCartType.ShoppingCart, ShoppingCartType.Wishlist };

        for (int i= 0; i< 2; i++)
        {
            if (carts[i].Any())
            {
                //Empty cart
                await EmptyCartAsync(customer.Id, types[i]);

                List<ShoppingCartItem> shoppingCartItemList = new();

                foreach (var item in carts[i])
                {
                    //New shopping cart item
                    var now = DateTime.UtcNow;

                    var shoppingCartItem = new ShoppingCartItem
                    {
                        ShoppingCartType = types[i],
                        StoreId = storeId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        CreatedOnUtc = now,
                        UpdatedOnUtc = now,
                        CustomerId = customer.Id
                    };

                    shoppingCartItemList.Add(shoppingCartItem);
                }

                if (shoppingCartItemList.Any())
                {
                    await _sciRepository.InsertAsync(shoppingCartItemList);
                }

                if (types[i] == ShoppingCartType.ShoppingCart)
                {
                    //updated "HasShoppingCartItems" property used for performance optimization
                    customer.HasShoppingCartItems = shoppingCartItemList.Any();

                    await _customerService.UpdateCustomerAsync(customer);
                }
            }
        }
        
        return warnings;
    }

    public async Task<List<string>> UpdateCartAsync(
        Customer customer,
        List<ShoppingCartItemPut> newItems,
        int storeId
    )
    {
        List<string> warnings = new();

        if (!newItems.Any())
        {
            warnings.Add("Must be at least one element to update");
            return warnings;
        }

        var fullCart = await _shoppingCartService.GetShoppingCartAsync( customer: customer, storeId: storeId);

        var query = from newItem in newItems
                   join fullCartItem in fullCart
                   on newItem.Id equals fullCartItem.Id
                   select new
                   {
                       ShoppingCartItemPost = new ShoppingCartItemPost
                       {
                           ProductId = fullCartItem.ProductId,
                           Quantity = newItem.Quantity,
                           ShoppingCartType = fullCartItem.ShoppingCartType
                       },

                       ShoppingCartItem = fullCartItem,
                   };

        var customCart = query.ToList();

        //Validate all elements be inside the cart
        if (!customCart.Any())
        {
            warnings.Add("All items must be inside the cart");
            return warnings;
        }

        List<ShoppingCartItemPost> shoppingCartItemPostList = customCart.Select(item => item.ShoppingCartItemPost).ToList();

        warnings = await GetShoppingCartItemWarningsAsync(customer, shoppingCartItemPostList);

        if (warnings.Any())
        {
            return warnings;
        }

        List<ShoppingCartItem> shoppingCartItemUpdateList = new();
        List<ShoppingCartItem> shoppingCartItemDeleteList = new();

        foreach (var newItem in customCart)
        {
            newItem.ShoppingCartItem.Quantity = newItem.ShoppingCartItemPost.Quantity;

            //Delete prodcuts without quantity
            if (newItem.ShoppingCartItem.Quantity == 0)
            {
                shoppingCartItemDeleteList.Add(newItem.ShoppingCartItem);
            }
            else
            {
                shoppingCartItemUpdateList.Add(newItem.ShoppingCartItem);
            }

        }

        //Delete
        if (shoppingCartItemDeleteList.Any())
        {
            await _sciRepository.DeleteAsync(shoppingCartItemDeleteList);
        }

        // Upadate
        if (shoppingCartItemUpdateList.Any())
        {
            await _sciRepository.UpdateAsync(shoppingCartItemUpdateList);
        }

        //No warnigns
        return warnings;
    }

    public async Task EmptyCartAsync(int customerId, ShoppingCartType shoppingCartType)
    {
        var cart = await GetShoppingCartItemsAsync(customerId: customerId, shoppingCartType: shoppingCartType);

        if (cart.Count == 0) return;

        await _sciRepository.DeleteAsync(cart);
    }

    #endregion

    #region Private methods

    private async Task<List<string>> GetShoppingCartItemWarningsAsync(Customer customer, List<ShoppingCartItemPost> shoppingCartItems)
    {
        var warnings = new List<string>();

        var shoppingCart = shoppingCartItems.Where(item => item.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
        var wishList = shoppingCartItems.Where(item => item.ShoppingCartType == ShoppingCartType.Wishlist).ToList();

        //Fatal wwarnings
        if (shoppingCart.Any() && !await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart, customer))
        {
            warnings.Add("Shopping cart is disabled");
            return warnings;
        }

        if (wishList.Any() && !await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableWishlist, customer))
        {
            warnings.Add("Wishlist is disabled");
            return warnings;
        }

        if (customer.IsSearchEngineAccount())
        {
            warnings.Add("Search engine can't add to cart");
            return warnings;
        }

        foreach (var item in shoppingCartItems)
        {
            if (item.Quantity <= 0)
            {
                warnings.Add($"item {item.ProductId} quantity should be positive");
                return warnings;
            }
        }

        var shoppingCartGroup = new Group
        (
            cart: shoppingCart,
            maximumItems: _shoppingCartSettings.MaximumShoppingCartItems,
            locale: "ShoppingCart.MaximumShoppingCartItems",
            shoppingCartType: ShoppingCartType.ShoppingCart
        );

        var wishListGroup = new Group
        (
            cart: wishList,
            maximumItems: _shoppingCartSettings.MaximumWishlistItems,
            locale: "ShoppingCart.MaximumWishlistItems",
            shoppingCartType: ShoppingCartType.Wishlist
        );

        var groupList = new List<Group> { shoppingCartGroup, wishListGroup };

        foreach ( var group in groupList ) 
        { 
            if (group.Cart.Any())
            {
                //maximum items validation
                if (group.Cart.Count >= group.MaximumItems)
                {
                    warnings.Add(string.Format(await _localizationService.GetResourceAsync(group.Locale), group.MaximumItems));
                    return warnings;
                }

                //Standard warnings
                var productIds = group.Cart.Select(x => x.ProductId).ToList();

                List<Product> products = await GetProductsFromIdList(productIds);

                warnings.AddRange(await GetStandardWarningsAsync(group.ShoppingCartType, products, group.Cart));
            }
        }

        return warnings;
    }

    private IQueryable<ShoppingCartItem> GetShoppingCartItemsQuery(
        int? customerId = null, DateTime? createdAtMin = null, DateTime? createdAtMax = null,
        DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, ShoppingCartType? shoppingCartType = null)
    {
        var query = _shoppingCartItemsRepository.Table;

        if (customerId != null)
        {
            query = query.Where(shoppingCartItem => shoppingCartItem.CustomerId == customerId);
        }

        if (createdAtMin != null)
        {
            query = query.Where(c => c.CreatedOnUtc > createdAtMin.Value);
        }

        if (createdAtMax != null)
        {
            query = query.Where(c => c.CreatedOnUtc < createdAtMax.Value);
        }

        if (updatedAtMin != null)
        {
            query = query.Where(c => c.UpdatedOnUtc > updatedAtMin.Value);
        }

        if (updatedAtMax != null)
        {
            query = query.Where(c => c.UpdatedOnUtc < updatedAtMax.Value);
        }

        if (shoppingCartType != null)
        {
            query = query.Where(c => c.ShoppingCartTypeId == (int)shoppingCartType.Value);
        }

        // items for the current store only
        var currentStoreId = _storeContext.GetCurrentStore().Id;
        query = query.Where(c => c.StoreId == currentStoreId);

        query = query.OrderBy(shoppingCartItem => shoppingCartItem.Id);

        return query;
    }

    
    private async Task<List<Product>> GetProductsFromIdList(IList<int> productIds)
    {
        var productQuery = from product in _productRepository.Table
                           where productIds.Contains(product.Id)
                           select product;

        return await productQuery.ToListAsync();
    }


    /// <summary>
    /// Validates a product for standard properties
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="shoppingCartType">Shopping cart type</param>
    /// <param name="product">Product</param>
    /// <param name="attributesXml">Attributes in XML format</param>
    /// <param name="customerEnteredPrice">Customer entered price</param>
    /// <param name="quantity">Quantity</param>
    /// <param name="shoppingCartItemId">Shopping cart identifier; pass 0 if it's a new item</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the warnings
    /// </returns>
    private async Task<List<string>> GetStandardWarningsAsync(ShoppingCartType shoppingCartType, List<Product> products,
        List<ShoppingCartItemPost> cart)
    {
        if (cart.Any(item => item.ShoppingCartType != shoppingCartType))
        {
            throw new ArgumentException(
                "Some products are not of the same shoppingCartType"
            );
        }

        //Locales
        string localeShoppingCartQuantityExceedsStock = await _localizationService.GetResourceAsync("ShoppingCart.QuantityExceedsStock");
        string localeShoppingCartMinimumQuantity = await _localizationService.GetResourceAsync("ShoppingCart.MinimumQuantity");
        string localeShoppingCartMaximumQuantity = await _localizationService.GetResourceAsync("ShoppingCart.MaximumQuantity");
        string localeShoppingCartAllowedQuantities = await _localizationService.GetResourceAsync("ShoppingCart.AllowedQuantities");
        string localeShoppingCartProductDeleted = await _localizationService.GetResourceAsync("ShoppingCart.ProductDeleted");
        string localeShoppingCartProductUnpublished = await _localizationService.GetResourceAsync("ShoppingCart.ProductUnpublished");
        string localeShoppingCartBuyingDisabled = await _localizationService.GetResourceAsync("ShoppingCart.BuyingDisabled");
        string localeShoppingCartWishlistDisabled = await _localizationService.GetResourceAsync("ShoppingCart.WishlistDisabled");

        var warnings = new List<string>();

        var productsWithQuantities = await GetTotalStockQuantityAsync(products);

        for (int i = 0;  i < cart.Count; i++)
        {
            var item = cart[i];

            Product? product = products.Find(p => p.Id == item.ProductId);

            if (product is null)
            {
                throw new ArgumentException("Some products are null in GetStandardWarningsAsync method");
            }

            if (shoppingCartType == ShoppingCartType.ShoppingCart && product.DisableBuyButton)
            {
                warnings.Add($"{i}:{localeShoppingCartBuyingDisabled}");
            }
            //disabled "add to wishlist" button
            else if (shoppingCartType == ShoppingCartType.Wishlist && product.DisableWishlistButton)
            {
                warnings.Add($"{i}:{localeShoppingCartWishlistDisabled}");
            }
            //deleted
            else if (product.Deleted)
            {
                warnings.Add($"{i}:{localeShoppingCartProductDeleted}");
            }
            //published
            else if (!product.Published)
            {
                warnings.Add($"{i}:{localeShoppingCartProductUnpublished}");
            }
            //we can add only simple products
            else if (product.ProductType != ProductType.SimpleProduct)
            {
                warnings.Add($"{i}:This is not simple product");
            }

            //quantity validation
            else
            {
                var hasQtyWarnings = false;
                if (item.Quantity < product.OrderMinimumQuantity)
                {
                    warnings.Add($"{i}:{string.Format(localeShoppingCartMinimumQuantity, product.OrderMinimumQuantity)}");
                    hasQtyWarnings = true;
                }

                if (item.Quantity > product.OrderMaximumQuantity)
                {
                    warnings.Add($"{i}:{string.Format(localeShoppingCartMaximumQuantity, product.OrderMaximumQuantity)}");
                    hasQtyWarnings = true;
                }

                var allowedQuantities = _productService.ParseAllowedQuantities(product);

                if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(item.Quantity))
                {
                    warnings.Add($"{i}:{string.Format(localeShoppingCartAllowedQuantities, string.Join(", ", allowedQuantities))}");
                }

                var validateOutOfStock = shoppingCartType == ShoppingCartType.ShoppingCart || !_shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
                if (validateOutOfStock && !hasQtyWarnings)
                {
                    switch (product.ManageInventoryMethod)
                    {
                        case ManageInventoryMethod.DontManageStock:
                            //do nothing
                            break;
                        case ManageInventoryMethod.ManageStock:
                            if (product.BackorderMode == BackorderMode.NoBackorders)
                            {
                                var maximumQuantityCanBeAdded = productsWithQuantities.Find(pwq => pwq.Product.Id == product.Id)?.StockQuantity;

                                if (maximumQuantityCanBeAdded is null)
                                {
                                    throw new ArgumentNullException(nameof(maximumQuantityCanBeAdded));
                                }

                                var warningList = GetQuantityProductWarningsAsync(item.Quantity, maximumQuantityCanBeAdded ?? 0, localeShoppingCartQuantityExceedsStock);

                                foreach(var warning in warningList)
                                {
                                    warnings.Add($"{i}:{warning}");
                                }
                            }

                            break;
                        case ManageInventoryMethod.ManageStockByAttributes:
                            //do nothing

                            break;
                        default:
                            break;
                    }
                }
            }
        }

        return warnings;
    }

    /// <summary>
    /// Get total quantity
    /// </summary>
    /// <param name="products">Product list</param>
    /// <param name="useReservedQuantity">
    /// A value indicating whether we should consider "Reserved Quantity" property 
    /// when "multiple warehouses" are used
    /// </param>
    /// <param name="warehouseId">
    /// Warehouse identifier. Used to limit result to certain warehouse.
    /// Used only with "multiple warehouses" enabled.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    private async Task<List<ProductWithStockQuantity>> GetTotalStockQuantityAsync(List<Product> products, bool useReservedQuantity = true, int warehouseId = 0)
    {
        List<ProductWithStockQuantity> list = new();

        var query = from wi in _productWarehouseInventoryRepository.Table
                    where products.Any(p => p.Id == wi.ProductId) &&
                        (warehouseId <= 0 || wi.WarehouseId == warehouseId)
                    select wi;

        var wiList = await query.ToListAsync();

        foreach (var product in products)
        {
            int quantity = 0;

            if (product.ManageInventoryMethod != ManageInventoryMethod.ManageStock)
            {
                //We can calculate total stock quantity when 'Manage inventory' property is set to 'Track inventory'
                quantity = 0;
            }
            else if (!product.UseMultipleWarehouses)
            {
                quantity = product.StockQuantity;
            }
            else
            {
                var pwi = wiList.FindAll(wi => wi.ProductId == product.Id);

                quantity = pwi.Sum(x => x.StockQuantity);

                if (useReservedQuantity)
                    quantity -= pwi.Sum(x => x.ReservedQuantity);
            }

            list.Add(new ProductWithStockQuantity(product, quantity));
        }

        return list;
    }


    /// <summary>
    /// Validates the maximum quantity a product can be added 
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="quantity">Quantity</param>
    /// <param name="maximumQuantityCanBeAdded">The maximum quantity a product can be added</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the warnings 
    /// </returns>
   private List<string> GetQuantityProductWarningsAsync(int quantity, int maximumQuantityCanBeAdded, string localeShoppingCartQuantityExceedsStock)
   {

        var warnings = new List<string>();

        if (maximumQuantityCanBeAdded < quantity)
        {
            if (maximumQuantityCanBeAdded <= 0)
            {
                var warning = "ShoppingCart.OutOfStock";
                warnings.Add(warning);
            }
            else
                warnings.Add(string.Format(localeShoppingCartQuantityExceedsStock, maximumQuantityCanBeAdded));
        }

        return warnings;
    }

    #endregion

    #region private classes

    private class ProductWithStockQuantity
    {
        public Product Product { get; set; }
        public int StockQuantity {  get; set; }

        public ProductWithStockQuantity(Product product, int stockQuantity)
        {
            Product = product;
            StockQuantity = stockQuantity;
        }
    }

    private record Group
    {
        public Group(List<ShoppingCartItemPost> cart, int maximumItems, string locale, ShoppingCartType shoppingCartType)
        {
            Cart = cart;
            MaximumItems = maximumItems;
            Locale = locale;
            ShoppingCartType = shoppingCartType;
        }

        public List<ShoppingCartItemPost> Cart { get; set; }
        public int MaximumItems { get; set; }
        public string Locale { get; set; }
        public ShoppingCartType ShoppingCartType { get; set; }
    }

    #endregion
}
