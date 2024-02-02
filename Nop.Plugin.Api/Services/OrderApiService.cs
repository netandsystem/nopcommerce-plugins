using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.Api.DataStructures;
using Nop.Plugin.Api.DTO.Orders;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Nop.Plugin.Api.Models;

namespace Nop.Plugin.Api.Services;

#nullable enable

public class OrderApiService : IOrderApiService
{
    #region Fields

    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<Address> _addressRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IProductApiService _productApiService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IPaymentService _paymentService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IShippingService _shippingService;
    private readonly ICustomerService _customerService;
    private readonly ICustomerApiService _customerApiService;
    private readonly ShippingSettings _shippingSettings;
    private readonly ILocalizationService _localizationService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ICurrencyService _currencyService;
    private readonly CurrencySettings _currencySettings;
    private readonly IWorkContext _workContext;
    private readonly IAddressService _addressService;
    private readonly TaxSettings _taxSettings;
    private readonly IWebHelper _webHelper;
    private readonly ICustomNumberFormatter _customNumberFormatter;
    private readonly IProductService _productService;
    private readonly ITaxPluginManager _taxPluginManager;
    private readonly ILogger _logger;
    private readonly ITaxCategoryService _taxCategoryService;
    private readonly IOrderService _orderService;
    private readonly ICustomerActivityService _customerActivityService;
    private readonly IEventPublisher _eventPublisher;

    #endregion

    #region Ctr

    public OrderApiService(
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Address> addresspository,
        IRepository<Product> productRepository,
        IProductApiService productApiService,
        IOrderProcessingService orderProcessingService,
        IPaymentService paymentService,
        IGenericAttributeService genericAttributeService,
        IShippingService shippingService,
        ICustomerService customerService,
        ShippingSettings shippingSettings,
        ICustomerApiService customerApiService,
        ILocalizationService localizationService,
        IRepository<Customer> customerRepository,
        IShoppingCartService shoppingCartService,
        ICurrencyService currencyService,
        CurrencySettings currencySettings,
        IWorkContext workContext,
        IAddressService addressService,
        TaxSettings taxSettings,
        IWebHelper webHelper,
        ICustomNumberFormatter customNumberFormatter,
        IProductService productService,
        ITaxPluginManager taxPluginManager,
        ILogger logger,
        ITaxCategoryService taxCategoryService,
        IOrderService orderService,
        ICustomerActivityService customerActivityService,
        IEventPublisher eventPublisher
    )
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _addressRepository = addresspository;
        _productRepository = productRepository;
        _productApiService = productApiService;
        _orderProcessingService = orderProcessingService;
        _paymentService = paymentService;
        _genericAttributeService = genericAttributeService;
        _shippingService = shippingService;
        _customerService = customerService;
        _shippingSettings = shippingSettings;
        _customerApiService = customerApiService;
        _localizationService = localizationService;
        _customerRepository = customerRepository;
        _shoppingCartService = shoppingCartService;
        _currencyService = currencyService;
        _currencySettings = currencySettings;
        _workContext = workContext;
        _addressService = addressService;
        _taxSettings = taxSettings;
        _webHelper = webHelper;
        _customNumberFormatter = customNumberFormatter;
        _productService = productService;
        _taxPluginManager = taxPluginManager;
        _logger = logger;
        _taxCategoryService = taxCategoryService;
        _orderService = orderService;
        _customerActivityService = customerActivityService;
        _eventPublisher = eventPublisher;
    }

    #endregion

    #region Methods

    public async Task<List<OrderDto>> GetOrders(
        int? customerId,
        int? limit,
        int? page,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        ShippingStatus? shippingStatus,
        int? storeId,
        bool orderByDateDesc,
        DateTime? createdAtMin,
        DateTime? createdAtMax,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    )
    {
        int limitValue = limit ?? Constants.Configurations.DefaultLimit;
        int pageValue = page ?? Constants.Configurations.DefaultPageValue;

        var ordersQuery = GetOrdersQuery(
                customerId: customerId,
                createdAtMin: createdAtMin,
                createdAtMax: createdAtMax,
                status: status,
                paymentStatus: paymentStatus,
                shippingStatus: shippingStatus,
                storeId: storeId,
                orderByDateDesc: orderByDateDesc,
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );

        var ordersItemQuery = from order in ordersQuery
                              join address in _addressRepository.Table
                              on order.BillingAddressId equals address.Id
                              join orderItem in _orderItemRepository.Table
                              on order.Id equals orderItem.OrderId
                              into orderItemsGroup
                              select order.ToDto(orderItemsGroup.ToList(), address, _paymentService.DeserializeCustomValues(order), null);

        var apiList = new ApiList<OrderDto>(ordersItemQuery, pageValue - 1, limitValue);

        var ordersDto = await apiList.ToListAsync();

        HashSet<int> productIds = new();

        foreach (var order in ordersDto)
        {
            var ids = order.OrderItems.Select(item => item.ProductId);

            foreach (var id in ids)
            {
                productIds.Add(id);
            }
        }

        var productsQuery = from product in _productRepository.Table
                            where productIds.Any(id => id == product.Id)
                            select product;

        var products = await productsQuery.ToListAsync();

        var productsDto = await _productApiService.JoinProductsAndPicturesAsync(products);

        foreach (var order in ordersDto)
        {
            foreach (var item in order.OrderItems)
            {
                item.Product = productsDto.Find(p => p.Id == item.ProductId);

                if (item.Product is null)
                {
                    throw new Exception("There are some products null in GetOrders");
                }
            }
        }

        return ordersDto;
    }

    //public async Task<List<OrderPost>> FilterNotCreatedOrdersPostAsync(IList<OrderPost> ordersPost)
    //{
    //    var query = from order in _orderRepository.Table
    //                where !ordersPost.Contains(x => x.) && order.Deleted == false
    //                select order.OrderGuid;

    //}

    public async Task<PlaceOrderResult> PlaceOrderAsync(OrderPost newOrder, Customer customer, int storeId, IList<ShoppingCartItem> cart)
    {
        newOrder.CustomValuesXml ??= new();

        if (newOrder.PaymentData is not null)
        {
            newOrder.CustomValuesXml.Add("Número de referencia", newOrder.PaymentData.ReferenceNumber);
            newOrder.CustomValuesXml.Add("Monto en Bs", newOrder.PaymentData.AmountInBs);
        }

        bool pickupInStore = newOrder.PickUpInStore ?? false;

        if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            if (pickupInStore && _shippingSettings.AllowPickupInStore)
            {
                //pickup point
                var response = await _shippingService.GetPickupPointsAsync(customer.BillingAddressId ?? 0,
                customer, "", storeId);

                if (!response.Success)
                {
                    return new PlaceOrderResult
                    {
                        Errors = response.Errors
                    };
                }

                var selectedPoint = response.PickupPoints.FirstOrDefault();

                await SavePickupOptionAsync(selectedPoint, customer, storeId);
            }
            else
            {
                if (newOrder.ShippingRateComputationMethodSystemName is null)
                {
                    throw new Exception("if pick_up_in_store is false then shipping_rate_computation_method_system_name cannot be null");
                }

                if (newOrder.ShippingMethod is null)
                {
                    throw new Exception("if pick_up_in_store is false then shipping_method cannot be null");
                }

                //set value indicating that "pick up in store" option has not been chosen
#nullable disable
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, storeId);
#nullable enable

                //find shipping method
                //performance optimization. try cache first
                var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(customer,
                    NopCustomerDefaults.OfferedShippingOptionsAttribute, storeId);
                if (shippingOptions == null || !shippingOptions.Any())
                {
                    var address = await _customerService.GetCustomerShippingAddressAsync(customer);
                    //not found? let's load them using shipping service
                    shippingOptions = (await _shippingService.GetShippingOptionsAsync(cart, address,
                        customer, newOrder.ShippingRateComputationMethodSystemName, storeId)).ShippingOptions.ToList();
                }
                else
                {
                    //loaded cached results. let's filter result by a chosen shipping rate computation method
                    shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(newOrder.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                var shippingOption = shippingOptions
                    .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(newOrder.ShippingMethod, StringComparison.InvariantCultureIgnoreCase));

                if (shippingOption == null)
                {
                    throw new Exception("shipping method not found");
                }

                //save
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, storeId);
            }

        }


        var processPaymentRequest = new ProcessPaymentRequest
        {
            StoreId = storeId,
            CustomerId = customer.Id,
            PaymentMethodSystemName = newOrder.PaymentMethodSystemName,
            OrderGuid = newOrder.OrderGuid ?? Guid.NewGuid(),
            OrderGuidGeneratedOnUtc = DateTime.UtcNow,
            CustomValues = newOrder.CustomValuesXml,
            OrderManagerGuid = newOrder.OrderManagerGuid,
            SellerId = newOrder.SellerId
        };

        //_paymentService.GenerateOrderGuid(processPaymentRequest);

        var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);

        if (placeOrderResult.Success)
        {
            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = placeOrderResult.PlacedOrder
            };

            await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);
        }

        return placeOrderResult;
    }

    public async Task<List<OrderDto>> GetLastestUpdatedItemsAsync(DateTime? lastUpdateUtc, int sellerId, int storeId)
    {
        return await GetOrders(
                customerId: null,
                limit: null,
                page: null,
                status: null,
                paymentStatus: null,
                shippingStatus: null,
                storeId: storeId,
                orderByDateDesc: false,
                createdAtMin: null,
                createdAtMax: null,
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );
    }


    public async Task<BaseSyncResponse> GetLastestUpdatedItems2Async(DateTime? lastUpdateUtc, int sellerId, int storeId)
    {
        // get date 12 months ago
        //var createdAtMin = DateTime.UtcNow.AddMonths(-12);

        var items = await GetLastedUpdatedOrders(lastUpdateUtc, sellerId);

        var itemsCompressed = GetItemsCompressed(items);

        return new BaseSyncResponse(itemsCompressed);
    }

    public async Task<List<OrderDto>> GetLastedUpdatedOrders(
        DateTime? lastUpdateUtc,
        int sellerId
    )
    {
        var ordersQuery = GetOrdersQuery(
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );

        var ordersDtoQuery = from order in ordersQuery
                             join customer in _customerRepository.Table
                             on order.CustomerId equals customer.Id
                             join address in _addressRepository.Table
                             on order.BillingAddressId equals address.Id
                             select order.ToDto(new List<OrderItem>(), address, _paymentService.DeserializeCustomValues(order), customer);

        var ordersDto = await ordersDtoQuery.ToListAsync();

        var customers = ordersDto.Select(x => x.Customer).ToList();

        await _customerApiService.JoinCustomerDtosWithCustomerAttributesAsync(customers);

        return ordersDto;
    }


    public async Task<List<OrderDto>> GetOrders2(
        int? customerId,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        ShippingStatus? shippingStatus,
        int? storeId,
        bool orderByDateDesc,
        DateTime? createdAtMin,
        DateTime? createdAtMax,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    )
    {
        var ordersQuery = GetOrdersQuery(
                customerId: customerId,
                createdAtMin: createdAtMin,
                createdAtMax: createdAtMax,
                status: status,
                paymentStatus: paymentStatus,
                shippingStatus: shippingStatus,
                storeId: storeId,
                orderByDateDesc: orderByDateDesc,
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );

        var ordersItemQuery = from order in ordersQuery
                              join customer in _customerRepository.Table
                              on order.CustomerId equals customer.Id
                              join address in _addressRepository.Table
                              on order.BillingAddressId equals address.Id
                              join orderItem in _orderItemRepository.Table
                              on order.Id equals orderItem.OrderId
                              into orderItemsGroup
                              select order.ToDto(orderItemsGroup.ToList(), address, _paymentService.DeserializeCustomValues(order), customer);


        var ordersDto = await ordersItemQuery.ToListAsync();

        HashSet<int> productIds = new();

        foreach (var order in ordersDto)
        {
            var ids = order.OrderItems.Select(item => item.ProductId);

            foreach (var id in ids)
            {
                productIds.Add(id);
            }
        }

        var productsQuery = from product in _productRepository.Table
                            where productIds.Any(id => id == product.Id)
                            select product.ToDto(null);

        var productsDto = await productsQuery.ToListAsync();

        var customersDto = ordersDto.Select(o => o.Customer).ToList();

        await _customerApiService.JoinCustomerDtosWithCustomerAttributesAsync(customersDto);

        foreach (var order in ordersDto)
        {
            foreach (var item in order.OrderItems)
            {
                item.Product = productsDto.Find(p => p.Id == item.ProductId);

                if (item.Product is null)
                {
                    throw new Exception("There are some products null in GetOrders");
                }
            }
        }

        return ordersDto;
    }

    public List<List<object?>> GetItemsCompressed(IList<OrderDto> items)
    {
        /*
        [
          id,   string
          deleted,  boolean
          updated_on_ts,  number

          created_on_ts,  number

          order_shipping_excl_tax,  number
          order_discount,  number
          custom_values,  json
      
          order_status,  string
          paid_date_ts,  number

          customer_id,  number
          customer_code: z.string().optional().nullable(),
          customer_business_name: z.string().optional().nullable(),
          customer_rif: z.string().optional().nullable(),

          billing_address_1: z.string().optional().nullable(),
          billing_address_2: z.string().optional().nullable(),
        ]
      */

        return items.Select(p =>
            new List<object?>() {
                p.Id,
                p.Deleted,
                p.UpdatedOnTs,

                p.CreatedOnTs,

                p.OrderShippingExclTax,
                p.OrderDiscount,
                p.CustomValues is null || p.CustomValues.Count == 0 ? null : p.CustomValues,

                p.OrderStatus,
                p.PaidDateTs,

                p.CustomerId,
                p.Customer?.SystemName,
                p.Customer?.Attributes?.GetValueOrDefault("company"),
                p.Customer?.Attributes?.GetValueOrDefault("rif"),

                p.BillingAddress.Address1,
                p.BillingAddress.Address2,
            }
        ).ToList();
    }


    /*

    public Order GetOrderById(int orderId)
    {
        if (orderId <= 0)
        {
            return null;
        }

        return _orderRepository.Table.FirstOrDefault(order => order.Id == orderId && !order.Deleted);
    }

    public int GetOrdersCount(
        DateTime? createdAtMin = null, DateTime? createdAtMax = null, OrderStatus? status = null,
        PaymentStatus? paymentStatus = null, ShippingStatus? shippingStatus = null,
        int? customerId = null, int? storeId = null)
    {
        var query = GetOrdersQuery(createdAtMin, createdAtMax, status, paymentStatus, shippingStatus, customerId: customerId, storeId: storeId);

        return query.Count();
    }

    */


    public async Task<CustomPlaceOrderResult> PlaceManyOrderAsync(Customer customer, int billingAddressId, Guid orderManagerGuid, IList<OrderPost2> orderPostList, int storeId)
    {
        var result = new CustomPlaceOrderResult();

        var uniqueOrderPostList = await GetUniqueOrders(orderPostList);

        try
        {
            // DB tasks
            var taxRate = await GetTaxRateAsync(customer, storeId);
            var productsIds = uniqueOrderPostList.SelectMany(x => x.OrderItems).Select(x => x.ProductId).ToArray();
            var productList = await _productService.GetProductsByIdsAsync(productsIds);

            var orderList = new List<Order>();
            var orderItemList = new List<OrderItem>();

            foreach (var orderPost in uniqueOrderPostList)
            {
                if (orderPost.OrderGuid == Guid.Empty)
                    throw new Exception("Order GUID is not generated");

                //prepare order details
                var generalDetails = await PrepareOrderGeneralDetailsAsync(customer, billingAddressId);
                var details = PreparePlaceOrderDetailsAsync(generalDetails, orderPost);

                //create order
                var order = CreateOrder(orderPost, details, storeId, orderManagerGuid);
                orderList.Add(order);

                var orderItems = CreateOrderItems(details, order, orderPost, taxRate, productList);
                orderItemList.AddRange(orderItems);
            }

            await _orderRepository.InsertAsync(orderList);
            await _orderItemRepository.InsertAsync(orderItemList);

            //inventory
            foreach (var item in orderItemList)
            {
                var product = productList.FirstOrDefault(x => x.Id == item.ProductId) ?? throw new Exception("Product not found while saving inventory");

                var ordersIds = orderList.Select(x => x.Id).ToList();

                await _productService.AdjustInventoryAsync(product, -item.Quantity, "",
                    $"place orders: {JsonSerializer.Serialize(ordersIds)}");
            }

            foreach (var order in orderList)
            {
                //notifications
                await _orderProcessingService.SendNotificationsAndSaveNotesAsync(order);

                //reset checkout data
                await _customerActivityService.InsertActivityAsync("PublicStore.PlaceOrder",
                                       string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.PlaceOrder"), order.Id), order);

                //raise event       
                await _eventPublisher.PublishAsync(new OrderPlacedEvent(order));
            }

            result.PlacedOrders = orderList;
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync(exc.Message, exc);
            result.AddError(exc.Message);
        }

        if (result.Success)
            return result;

        //log errors
        var logError = result.Errors.Aggregate("Error while placing order. ",
            (current, next) => $"{current}Error {result.Errors.IndexOf(next) + 1}: {next}. ");
        await _logger.ErrorAsync(logError, customer: customer);

        return result;
    }


    #endregion

    #region Private methods
    private IQueryable<Order> GetOrdersQuery(
        int? customerId = null,
        DateTime? createdAtMin = null,
        DateTime? createdAtMax = null,
        OrderStatus? status = null,
        PaymentStatus? paymentStatus = null,
        ShippingStatus? shippingStatus = null,
        int? storeId = null,
        bool orderByDateDesc = false,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    )
    {
        var query = _orderRepository.Table;

        if (customerId != null)
        {
            query = query.Where(order => order.CustomerId == customerId);
        }

        if (status != null)
        {
            query = query.Where(order => order.OrderStatusId == (int)status);
        }

        if (paymentStatus != null)
        {
            query = query.Where(order => order.PaymentStatusId == (int)paymentStatus);
        }

        if (shippingStatus != null)
        {
            query = query.Where(order => order.ShippingStatusId == (int)shippingStatus);
        }

        query = query.Where(order => !order.Deleted);

        if (createdAtMin != null)
        {
            query = query.Where(order => order.CreatedOnUtc > createdAtMin.Value.ToUniversalTime());
        }

        if (createdAtMax != null)
        {
            query = query.Where(order => order.CreatedOnUtc < createdAtMax.Value.ToUniversalTime());
        }

        if (storeId != null)
        {
            query = query.Where(order => order.StoreId == storeId);
        }

        if (orderByDateDesc)
        {
            query = query.OrderByDescending(order => order.CreatedOnUtc);
        }
        else
        {
            query = query.OrderBy(order => order.Id);
        }

        if (sellerId != null)
        {
            query = query.Where(order => order.SellerId == sellerId);
        }

        if (lastUpdateUtc != null)
        {
            query = query.Where(order => order.UpdatedOnUtc > lastUpdateUtc);
        }

        return query;
    }

    private async Task SavePickupOptionAsync(PickupPoint? pickupPoint, Customer customer, int storeId)
    {
        if (pickupPoint == null)
        {
            throw new ArgumentNullException(nameof(pickupPoint));
        }

        var name = !string.IsNullOrEmpty(pickupPoint.Name) ?
            string.Format(await _localizationService.GetResourceAsync("Checkout.PickupPoints.Name"), pickupPoint.Name) :
            await _localizationService.GetResourceAsync("Checkout.PickupPoints.NullName");
        var pickUpInStoreShippingOption = new ShippingOption
        {
            Name = name,
            Rate = pickupPoint.PickupFee,
            Description = pickupPoint.Description,
            ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
            IsPickupInStore = true
        };

        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, storeId);
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, storeId);
    }

    private PlaceOrderContainer PreparePlaceOrderDetailsAsync(
        PlaceOrderContainer generalDetails,
        OrderPost2 orderPost
    )
    {
        var details = Copy(generalDetails);

        //************* Order Totals *************

        //tax display type
        details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

        //sub total (incl tax)
        details.OrderSubTotalInclTax = orderPost.OrderSubtotalInclTax;
        details.OrderSubTotalDiscountInclTax = 0;

        //sub total (excl tax)
        details.OrderSubTotalExclTax = orderPost.OrderSubtotalExclTax;
        details.OrderSubTotalDiscountExclTax = 0;

        //shipping total
        details.OrderShippingTotalInclTax = 0;
        details.OrderShippingTotalExclTax = 0;

        //payment total
        details.PaymentAdditionalFeeInclTax = 0;
        details.PaymentAdditionalFeeExclTax = 0;

        //tax amount
        details.OrderTaxTotal = orderPost.OrderTax;

        //tax rates
        details.TaxRates = "";

        details.OrderDiscountAmount = 0;
        details.RedeemedRewardPoints = 0;
        details.RedeemedRewardPointsAmount = 0;
        details.AppliedGiftCards = new List<AppliedGiftCard>();
        details.OrderTotal = orderPost.OrderTotal;

        return details;
    }

    private static T Copy<T>(T original)
    {
        T copy = Activator.CreateInstance<T>();
        foreach (var property in typeof(T).GetProperties())
        {
            property.SetValue(copy, property.GetValue(original));
        }
        return copy;
    }

    private async Task<PlaceOrderContainer> PrepareOrderGeneralDetailsAsync(
        Customer customer,
        int billingAddressId
    )
    {
        var details = new PlaceOrderContainer();

        //************* Customer *************

        //check whether customer is guest
        if (await _customerService.IsGuestAsync(customer))
            throw new NopException("Anonymous checkout is not allowed");

        details.Customer = customer;

        //************* Currency *************
        var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        details.CustomerCurrencyCode = primaryStoreCurrency.CurrencyCode;
        details.CustomerCurrencyRate = primaryStoreCurrency.Rate;

        //************* customer language *************
        details.CustomerLanguage = await _workContext.GetWorkingLanguageAsync();

        //************* billing address *************

        // is order 0?
        if (billingAddressId == 0)
        {
            throw new NopException("Billing address is 0");
        }

        // address belongs to customer?
        var addressValidation = await _customerApiService.GetCustomerAddressAsync(customer.Id, billingAddressId);

        if (addressValidation is null)
        {
            var addresses = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);
            var address = addresses.FirstOrDefault() ?? throw new NopException("he customer does not have a billing address");
            billingAddressId = address.Id;
        }

        customer.BillingAddressId = billingAddressId;
        customer.ShippingAddressId = billingAddressId;

        await _customerService.UpdateCustomerAsync(customer);
        var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
        details.BillingAddress = _addressService.CloneAddress(billingAddress);

        //************* Shipping *****************
        details.PickupInStore = false;
        details.ShippingStatus = ShippingStatus.ShippingNotRequired;
#nullable disable
        details.ShippingRateComputationMethodSystemName = null;
#nullable enable

        return details;
    }

    private Order CreateOrder(OrderPost2 orderPost, PlaceOrderContainer details, int storeId, Guid orderManagerGuid)
    {
        if (details.BillingAddress is null)
            throw new NopException("Billing address is not provided");

        var processPaymentRequest = new ProcessPaymentRequest
        {
            StoreId = storeId,
            CustomerId = details.Customer.Id,
            PaymentMethodSystemName = orderPost.PaymentMethodSystemName,
            OrderGuid = orderPost.OrderGuid,
            OrderGuidGeneratedOnUtc = DateTime.UtcNow,
            CustomValues = orderPost.CustomValuesXml,
            OrderManagerGuid = orderManagerGuid,
            SellerId = orderPost.SellerId
        };

        var order = new Order
        {
            StoreId = storeId,
            OrderGuid = orderPost.OrderGuid,
            CustomerId = details.Customer.Id,
            CustomerLanguageId = details.CustomerLanguage.Id,
            CustomerTaxDisplayType = details.CustomerTaxDisplayType,
            CustomerIp = _webHelper.GetCurrentIpAddress(),
            OrderSubtotalInclTax = details.OrderSubTotalInclTax,
            OrderSubtotalExclTax = details.OrderSubTotalExclTax,
            OrderSubTotalDiscountInclTax = details.OrderSubTotalDiscountInclTax,
            OrderSubTotalDiscountExclTax = details.OrderSubTotalDiscountExclTax,
            OrderShippingInclTax = details.OrderShippingTotalInclTax,
            OrderShippingExclTax = details.OrderShippingTotalExclTax,
            PaymentMethodAdditionalFeeInclTax = details.PaymentAdditionalFeeInclTax,
            PaymentMethodAdditionalFeeExclTax = details.PaymentAdditionalFeeExclTax,
            TaxRates = details.TaxRates,
            OrderTax = details.OrderTaxTotal,
            OrderTotal = details.OrderTotal,
            RefundedAmount = decimal.Zero,
            OrderDiscount = details.OrderDiscountAmount,
            CheckoutAttributeDescription = details.CheckoutAttributeDescription,
            CheckoutAttributesXml = details.CheckoutAttributesXml,
            CustomerCurrencyCode = details.CustomerCurrencyCode,
            CurrencyRate = details.CustomerCurrencyRate,
            AffiliateId = details.AffiliateId,
            OrderStatus = OrderStatus.Pending,
            AllowStoringCreditCardNumber = false,
            CardType = string.Empty,
            CardName = string.Empty,
            CardNumber = string.Empty,
            MaskedCreditCardNumber = string.Empty,
            CardCvv2 = string.Empty,
            CardExpirationMonth = string.Empty,
            CardExpirationYear = string.Empty,
            PaymentMethodSystemName = orderPost.PaymentMethodSystemName,
            AuthorizationTransactionId = null,
            AuthorizationTransactionCode = null,
            AuthorizationTransactionResult = null,
            CaptureTransactionId = null,
            CaptureTransactionResult = null,
            SubscriptionTransactionId = null,
            PaymentStatus = PaymentStatus.Pending,
            PaidDateUtc = null,
            PickupInStore = details.PickupInStore,
            ShippingStatus = details.ShippingStatus,
            ShippingMethod = details.ShippingMethodName,
            ShippingRateComputationMethodSystemName = details.ShippingRateComputationMethodSystemName,
            CustomValuesXml = _paymentService.SerializeCustomValues(processPaymentRequest),
            VatNumber = details.VatNumber,
            CreatedOnUtc = DateTime.UtcNow,
            CustomOrderNumber = string.Empty,
            // NaS Code
            OrderManagerGuid = processPaymentRequest.OrderManagerGuid,
            SellerId = processPaymentRequest.SellerId,
            UpdatedOnUtc = DateTime.UtcNow,
            // NaS Code
            PickupAddressId = null
        };



        //generate and set custom order number
        order.CustomOrderNumber = _customNumberFormatter.GenerateOrderCustomNumber(order);


        return order;
    }

    private List<OrderItem> CreateOrderItems(PlaceOrderContainer details, Order order, OrderPost2 orderPost, decimal taxRate, IList<Product> productList)
    {
        var orderItems = new List<OrderItem>();

        if (orderPost.OrderItems.Count == 0)
            throw new NopException("Order items are not provided");

        foreach (var sc in orderPost.OrderItems)
        {
            var product = productList.FirstOrDefault(x => x.Id == sc.ProductId) ?? throw new NopException("Product not found");

            var unitPriceInclTax = product.Price * (1 + taxRate / 100);
            var unitPriceExclTax = product.Price;
            var priceInclTax = unitPriceInclTax * sc.Quantity;
            var priceExclTax = unitPriceExclTax * sc.Quantity;

            //save order item
            var orderItem = new OrderItem
            {
                OrderItemGuid = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                UnitPriceInclTax = unitPriceInclTax,
                UnitPriceExclTax = unitPriceExclTax,
                PriceInclTax = priceInclTax,
                PriceExclTax = priceExclTax,
                OriginalProductCost = 0,
                AttributeDescription = null,
                AttributesXml = null,
                Quantity = sc.Quantity,
                DiscountAmountInclTax = 0,
                DiscountAmountExclTax = 0,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = 0,
                ItemWeight = 0,
                RentalStartDateUtc = null,
                RentalEndDateUtc = null
            };

            orderItems.Add(orderItem);
        }

        return orderItems;
    }


    private async Task<List<OrderPost2>> GetUniqueOrders(IList<OrderPost2> orderPostList)
    {
        // get unique orders
        var orderMap = new Dictionary<Guid, OrderPost2>();

        foreach (var newOrderPost in orderPostList)
        {
            orderMap.Add((Guid)newOrderPost.OrderGuid, newOrderPost);
        }

        var uniqueOrderPostList = orderMap.Values.ToList();

        var query = from orderPost in uniqueOrderPostList
                    join order in _orderRepository.Table
                    on orderPost.OrderGuid equals order.OrderGuid into orderGroup
                    from joinendOrder in orderGroup.DefaultIfEmpty()
                    where joinendOrder?.Id is null
                    select orderPost;

        return await query.ToListAsync();
    }

    protected virtual async Task<decimal> GetTaxRateAsync(
        Customer customer,
        int storeId
    )
    {
        var taxCategories = await _taxCategoryService.GetAllTaxCategoriesAsync() ?? throw new NopException("No tax category found");

        // encontrar categoria cuyo Name es WithTaxes
        var taxCategory = taxCategories.First(x => string.Compare(x.Name, "WithTaxes", StringComparison.OrdinalIgnoreCase) == 0) ?? throw new NopException("Tax category WithTaxes is not registered");

        var taxRate = decimal.Zero;

        //active tax provider
        var activeTaxProvider = await _taxPluginManager.LoadPrimaryPluginAsync(customer, storeId) ?? throw new NopException("No active tax provider found");


        //tax request
        var taxRateRequest = new TaxRateRequest
        {
            Customer = customer,
            TaxCategoryId = taxCategory.Id,
            CurrentStoreId = storeId
        };


        //get tax rate
        var taxRateResult = await activeTaxProvider.GetTaxRateAsync(taxRateRequest);

        if (taxRateResult.Success)
        {
            //ensure that tax is equal or greater than zero
            if (taxRateResult.TaxRate < decimal.Zero)
                taxRateResult.TaxRate = decimal.Zero;

            taxRate = taxRateResult.TaxRate;
        }
        else if (_taxSettings.LogErrors)
            foreach (var error in taxRateResult.Errors)
                await _logger.ErrorAsync($"{activeTaxProvider.PluginDescriptor.FriendlyName} - {error}", null, customer);

        return taxRate;
    }

    #endregion

    #region Nested classes

#nullable disable
    /// <summary>
    /// PlaceOrder container
    /// </summary>
    protected class PlaceOrderContainer
    {
        public PlaceOrderContainer()
        {
            Cart = new List<ShoppingCartItem>();
            AppliedDiscounts = new List<Discount>();
            AppliedGiftCards = new List<AppliedGiftCard>();
        }

        /// <summary>
        /// Customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Customer language
        /// </summary>
        public Language CustomerLanguage { get; set; }

        /// <summary>
        /// Affiliate identifier
        /// </summary>
        public int AffiliateId { get; set; }

        /// <summary>
        /// TAx display type
        /// </summary>
        public TaxDisplayType CustomerTaxDisplayType { get; set; }

        /// <summary>
        /// Selected currency
        /// </summary>
        public string CustomerCurrencyCode { get; set; }

        /// <summary>
        /// Customer currency rate
        /// </summary>
        public decimal CustomerCurrencyRate { get; set; }

        /// <summary>
        /// Billing address
        /// </summary>
        public Address BillingAddress { get; set; }

        /// <summary>
        /// Shipping address
        /// </summary>
        public Address ShippingAddress { get; set; }

        /// <summary>
        /// Shipping status
        /// </summary>
        public ShippingStatus ShippingStatus { get; set; }

        /// <summary>
        /// Selected shipping method
        /// </summary>
        public string ShippingMethodName { get; set; }

        /// <summary>
        /// Shipping rate computation method system name
        /// </summary>
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Is pickup in store selected?
        /// </summary>
        public bool PickupInStore { get; set; }

        /// <summary>
        /// Selected pickup address
        /// </summary>
        public Address PickupAddress { get; set; }

        /// <summary>
        /// Is recurring shopping cart
        /// </summary>
        public bool IsRecurringShoppingCart { get; set; }

        /// <summary>
        /// Initial order (used with recurring payments)
        /// </summary>
        public Order InitialOrder { get; set; }

        /// <summary>
        /// Checkout attributes
        /// </summary>
        public string CheckoutAttributeDescription { get; set; }

        /// <summary>
        /// Shopping cart
        /// </summary>
        public string CheckoutAttributesXml { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<ShoppingCartItem> Cart { get; set; }

        /// <summary>
        /// Applied discounts
        /// </summary>
        public List<Discount> AppliedDiscounts { get; set; }

        /// <summary>
        /// Applied gift cards
        /// </summary>
        public List<AppliedGiftCard> AppliedGiftCards { get; set; }

        /// <summary>
        /// Order subtotal (incl tax)
        /// </summary>
        public decimal OrderSubTotalInclTax { get; set; }

        /// <summary>
        /// Order subtotal (excl tax)
        /// </summary>
        public decimal OrderSubTotalExclTax { get; set; }

        /// <summary>
        /// Subtotal discount (incl tax)
        /// </summary>
        public decimal OrderSubTotalDiscountInclTax { get; set; }

        /// <summary>
        /// Subtotal discount (excl tax)
        /// </summary>
        public decimal OrderSubTotalDiscountExclTax { get; set; }

        /// <summary>
        /// Shipping (incl tax)
        /// </summary>
        public decimal OrderShippingTotalInclTax { get; set; }

        /// <summary>
        /// Shipping (excl tax)
        /// </summary>
        public decimal OrderShippingTotalExclTax { get; set; }

        /// <summary>
        /// Payment additional fee (incl tax)
        /// </summary>
        public decimal PaymentAdditionalFeeInclTax { get; set; }

        /// <summary>
        /// Payment additional fee (excl tax)
        /// </summary>
        public decimal PaymentAdditionalFeeExclTax { get; set; }

        /// <summary>
        /// Tax
        /// </summary>
        public decimal OrderTaxTotal { get; set; }

        /// <summary>
        /// VAT number
        /// </summary>
        public string VatNumber { get; set; }

        /// <summary>
        /// Tax rates
        /// </summary>
        public string TaxRates { get; set; }

        /// <summary>
        /// Order total discount amount
        /// </summary>
        public decimal OrderDiscountAmount { get; set; }

        /// <summary>
        /// Redeemed reward points
        /// </summary>
        public int RedeemedRewardPoints { get; set; }

        /// <summary>
        /// Redeemed reward points amount
        /// </summary>
        public decimal RedeemedRewardPointsAmount { get; set; }

        /// <summary>
        /// Order total
        /// </summary>
        public decimal OrderTotal { get; set; }
    }

    #endregion
}
