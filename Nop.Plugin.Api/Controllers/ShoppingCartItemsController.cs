using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.ShoppingCarts;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.ModelBinders;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.DTOs.ShoppingCarts;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Policies;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/shopping_cart_items")]
[Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
public class ShoppingCartItemsController : BaseApiController
{
    #region Fields
    private const string GENERAL_ERROR_HEADER = "shopping_cart_items";
    private const string BATCH_ERROR_HEADER = "shopping_cart_items_batch";

    private readonly IProductService _productService;
    private readonly IShoppingCartItemApiService _shoppingCartItemApiService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IStoreContext _storeContext;
    private readonly IPermissionService _permissionService;
    private readonly IAuthenticationService _authenticationService;

    #endregion

    #region Ctr
    public ShoppingCartItemsController(
        IShoppingCartItemApiService shoppingCartItemApiService,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IShoppingCartService shoppingCartService,
        IProductService productService,
        IPictureService pictureService,
        IStoreContext storeContext,
        IPermissionService permissionService,
        IAuthenticationService authenticationService)
        : base(jsonFieldsSerializer,
               aclService,
               customerService,
               storeMappingService,
               storeService,
               discountService,
               customerActivityService,
               localizationService,
               pictureService)
    {
        _shoppingCartItemApiService = shoppingCartItemApiService;
        _shoppingCartService = shoppingCartService;
        _productService = productService;
        _storeContext = storeContext;
        _permissionService = permissionService;
        _authenticationService = authenticationService;
    }


    #endregion

    /// <summary>
    ///     Receive a list of all shopping cart items of current customer
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetCurrentShoppingCart")]
    [ProducesResponseType(typeof(ShoppingCartItemsRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetCurrentShoppingCart(ShoppingCartType? shoppingCartType, string? fields)
    {
        if (shoppingCartType is null)
        {
            return BadRequest("shoppingCartType can't be null");
        }

        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id, shoppingCartType))
        {
            return AccessDenied();
        }

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(shoppingCartType ?? ShoppingCartType.ShoppingCart, customer);

        return OkResult(shoppingCartsRootObject, fields);
    }

    [HttpPost(Name = "CreateShoppingCartItem")]
    [ProducesResponseType(typeof(ShoppingCartItemsRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> CreateShoppingCartItem(
        ShoppingCartItemPost shoppingCartItemPost)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id, shoppingCartItemPost.ShoppingCartType))
        {
            return AccessDenied();
        }

        var product = await _productService.GetProductByIdAsync(shoppingCartItemPost.ProductId);

        if (product == null)
        {
            return Error(HttpStatusCode.NotFound, "product", "not found");
        }

        var currentStoreId = _storeContext.GetCurrentStore().Id;

        var warnings = await _shoppingCartService.AddToCartAsync(
            customer: customer,
            product: product, 
            shoppingCartType: shoppingCartItemPost.ShoppingCartType,
            storeId: currentStoreId,
            quantity: shoppingCartItemPost.Quantity
        );

        if (warnings.Count > 0)
        {
            foreach (var warning in warnings)
            {
                ModelState.AddModelError(GENERAL_ERROR_HEADER, warning);
            }

            return Error(HttpStatusCode.BadRequest);
        }

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(shoppingCartItemPost.ShoppingCartType, customer);

        return OkResult(shoppingCartsRootObject);
    }


    [HttpPost("batch", Name = "BatchCreateShoppingCartItems")]
    [ProducesResponseType(typeof(ShoppingCartItemsRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> BatchCreateShoppingCartItems(
        List<ShoppingCartItemPost> newItems
    )
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var areShoppingCartItems = newItems.Any(item => item.ShoppingCartType == ShoppingCartType.ShoppingCart);
        var areWhishListItems = newItems.Any(item => item.ShoppingCartType == ShoppingCartType.Wishlist);

        if (areShoppingCartItems && !await CheckPermissions(customer.Id, ShoppingCartType.ShoppingCart))
        {
            return AccessDenied();
        }

        if (areWhishListItems && !await CheckPermissions(customer.Id, ShoppingCartType.Wishlist))
        {
            return AccessDenied();
        }

        var store = _storeContext.GetCurrentStore();

        var warnings = await _shoppingCartItemApiService.AddProductListToCartAsync(customer, newItems, store.Id);

        var error = GetWarningBatchErrors(warnings);
        if (error is not null) return error;

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(null, customer);

        return OkResult(shoppingCartsRootObject);
    }

    [HttpPost("batch/replace", Name = "BatchReplaceShoppingCartItems")]
    [ProducesResponseType(typeof(ShoppingCartItemsRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> BatchReplaceShoppingCartItems(
        List<ShoppingCartItemPost> newItems
    )
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id, null))
        {
            return AccessDenied();
        }

        var store = _storeContext.GetCurrentStore();

        var warnings = await _shoppingCartItemApiService.ReplaceCartAsync(customer, newItems, store.Id);

        var error = GetWarningBatchErrors(warnings);
        if (error is not null) return error;

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(null, customer);

        return OkResult(shoppingCartsRootObject);
    }

    [HttpPut(Name = "UpdateShoppingCartItem")]
    [ProducesResponseType(typeof(ShoppingCartItemsRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> UpdateShoppingCartItem(ShoppingCartItemPut shoppingCartItemPut)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id, null))
        {
            return AccessDenied();
        }

        // We kno that the id will be valid integer because the validation for this happens in the validator which is executed by the model binder.
        var shoppingCartItemForUpdate = await _shoppingCartItemApiService.GetShoppingCartItemAsync(shoppingCartItemPut.Id);

        if (shoppingCartItemForUpdate == null)
        {
            return Error(HttpStatusCode.NotFound, "shopping_cart_item", "not found");
        }

        // The update time is set in the service.
        var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(
            customer,
            shoppingCartItemPut.Id,
            shoppingCartItemForUpdate.AttributesXml, 
            shoppingCartItemForUpdate.CustomerEnteredPrice,
            shoppingCartItemForUpdate.RentalStartDateUtc, 
            shoppingCartItemForUpdate.RentalEndDateUtc,
            shoppingCartItemPut.Quantity
        );

        var error = GetWarningBatchErrors(warnings);
        if (error is not null) return error;

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(shoppingCartItemForUpdate.ShoppingCartType, customer);

        return OkResult(shoppingCartsRootObject);
    }

    [HttpPut("batch", Name = "BatchUpdateShoppingCartItems")]
    [ProducesResponseType(typeof(ShoppingCartItemsRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> BatchUpdateShoppingCartItems(List<ShoppingCartItemPut> shoppingCartItemPutList)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id, null))
        {
            return AccessDenied();
        }

        var store = _storeContext.GetCurrentStore();

        var warnings = await _shoppingCartItemApiService.UpdateCartAsync(customer, shoppingCartItemPutList, store.Id);

        var error = GetWarningBatchErrors(warnings);
        if (error is not null) return error;

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(null, customer);

        return OkResult(shoppingCartsRootObject);
    }


    [HttpDelete("{id}", Name = "DeleteShoppingCartItem")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> DeleteShoppingCartItem([FromRoute] int id)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (id <= 0)
        {
            return Error(HttpStatusCode.BadRequest, "id", "invalid id");
        }

        var shoppingCartItemForDelete = await _shoppingCartItemApiService.GetShoppingCartItemAsync(id);

        if (shoppingCartItemForDelete is null)
        {
            return Error(HttpStatusCode.BadRequest, "id", "The specified item couldn't be found");
        }

        if (!await CheckPermissions(shoppingCartItemForDelete.CustomerId, shoppingCartItemForDelete.ShoppingCartType))
        {
            return AccessDenied();
        }

        await _shoppingCartService.DeleteShoppingCartItemAsync(shoppingCartItemForDelete);

        //activity log
        await CustomerActivityService.InsertActivityAsync("DeleteShoppingCartItem", await LocalizationService.GetResourceAsync("ActivityLog.DeleteShoppingCartItem"), shoppingCartItemForDelete);

        // load current shopping cart and return it as result of request
        var shoppingCartsRootObject = await LoadCurrentShoppingCartItems(shoppingCartItemForDelete.ShoppingCartType, customer);

        return OkResult(shoppingCartsRootObject);
    }

    [HttpDelete("clear_cart/{shoppingCartType}", Name = "ClearCart")]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> ClearCart([FromRoute] ShoppingCartType shoppingCartType)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id, shoppingCartType))
        {
            return AccessDenied();
        }

        await _shoppingCartItemApiService.EmptyCartAsync(customer.Id, (ShoppingCartType) shoppingCartType);

        return Ok();
    }

    #region Private methods

    private async Task<bool> CheckPermissions(int? customerId, ShoppingCartType? shoppingCartType)
    {
        var currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync();
        if (currentCustomer is null) // authenticated, but does not exist in db
            return false;
        if (customerId.HasValue && currentCustomer.Id == customerId)
        {
            // if I want to handle my own shopping cart, check only public store permission
            switch (shoppingCartType)
            {
                case ShoppingCartType.ShoppingCart:
                    return await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart, currentCustomer);
                case ShoppingCartType.Wishlist:
                    return await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableWishlist, currentCustomer);
                default:
                    return await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart, currentCustomer)
                        && await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableWishlist, currentCustomer);
            }
        }
        // if I want to handle other customer's shopping carts, check admin permission
        return await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCurrentCarts, currentCustomer);
    }

    private async Task<ShoppingCartItemsRootObject> LoadCurrentShoppingCartItems(ShoppingCartType? shoppingCartType, Customer customer)
    {
        var shoppingCart = await _shoppingCartItemApiService.GetShoppingCartItemsAsync(customerId: customer.Id, shoppingCartType: shoppingCartType);

        var shoppingCartDto = await _shoppingCartItemApiService.JoinShoppingCartItemsWithProductsAsync(shoppingCart);

        var shoppingCartsRootObject = new ShoppingCartItemsRootObject()
        {
            ShoppingCartItems = shoppingCartDto
        };

        return shoppingCartsRootObject;
    }

    private IActionResult? GetWarningBatchErrors(IList<string>? warnings)
    {
        if (warnings is not null && warnings.Any())
        {
            if (!warnings.All(w => w.Contains(':'))) 
            {
                ModelState.AddModelError(GENERAL_ERROR_HEADER, warnings[0]);
                return Error(HttpStatusCode.BadRequest);
            }

            foreach (var warning in warnings)
            {
                ModelState.AddModelError(BATCH_ERROR_HEADER, warning);
            }

            return Error(HttpStatusCode.BadRequest);
        }

        return null;
    }

    #endregion
}
