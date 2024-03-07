using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.OrderItems;
using Nop.Plugin.Api.DTO.Orders;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.ModelBinders;
using Nop.Plugin.Api.Models.OrdersParameters;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Plugin.Api.Models.Base;
using Nop.Plugin.Api.DTOs.Base;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/orders")]
public class OrdersController : BaseSyncController<OrderDto>
{
    #region Fields

    private const string GENERAL_ERROR_HEADER = "orders_items";
    private const string BATCH_ERROR_HEADER = "orders_items";
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IOrderApiService _orderApiService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IOrderService _orderService;
    private readonly IProductAttributeConverter _productAttributeConverter;
    private readonly IPaymentService _paymentService;
    private readonly IPdfService _pdfService;
    private readonly IPermissionService _permissionService;
    private readonly IProductService _productService;
    private readonly IShippingService _shippingService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ITaxPluginManager _taxPluginManager;
    private readonly IShoppingCartItemApiService _shoppingCartItemApiService;
    private readonly ICustomerApiService _customerApiService;
    private readonly OrderSettings _orderSettings;


    // We resolve the order settings this way because of the tests.
    // The auto mocking does not support concreate types as dependencies. It supports only interfaces.
    //private OrderSettings _orderSettings;

    #endregion

    #region Ctr
    public OrdersController(
        IOrderApiService orderApiService,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IProductService productService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IShoppingCartService shoppingCartService,
        IGenericAttributeService genericAttributeService,
        IStoreContext storeContext,
        IShippingService shippingService,
        IPictureService pictureService,
        IProductAttributeConverter productAttributeConverter,
        IPaymentService paymentService,
        IPdfService pdfService,
        IPermissionService permissionService,
        IAuthenticationService authenticationService,
        ITaxPluginManager taxPluginManager,
        IShoppingCartItemApiService shoppingCartItemApiService,
        ICustomerApiService customerApiService,
        OrderSettings orderSettings
        )
        : base(orderApiService, jsonFieldsSerializer, aclService, customerService, storeMappingService,
               storeService, discountService, customerActivityService, localizationService, pictureService, authenticationService, storeContext)
    {
        _orderApiService = orderApiService;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _shoppingCartService = shoppingCartService;
        _genericAttributeService = genericAttributeService;
        _shippingService = shippingService;
        _productService = productService;
        _productAttributeConverter = productAttributeConverter;
        _paymentService = paymentService;
        _pdfService = pdfService;
        _permissionService = permissionService;
        _taxPluginManager = taxPluginManager;
        _shoppingCartItemApiService = shoppingCartItemApiService;
        _customerApiService = customerApiService;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Receive a list of all Orders
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetOrders")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetOrders([FromQuery] OrdersParametersModel parameters)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id))
        {
            return AccessDenied();
        }

        if (parameters.Page < Constants.Configurations.DefaultPageValue)
        {
            return Error(HttpStatusCode.BadRequest, "page", "Invalid page parameter");
        }

        if (parameters.Limit < Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
        {
            return Error(HttpStatusCode.BadRequest, "limit", "Invalid limit parameter");
        }

        var storeId = _storeContext.GetCurrentStore().Id;

        IList<OrderDto> ordersDto = await _orderApiService.GetOrders(
                customerId: customer.Id,
                limit: parameters.Limit,
                page: parameters.Page,
                status: parameters.Status,
                paymentStatus: parameters.PaymentStatus,
                shippingStatus: parameters.ShippingStatus,
                storeId: storeId,
                orderByDateDesc: parameters.OrderByDateDesc,
                createdAtMin: parameters.CreatedAtMin,
                createdAtMax: parameters.CreatedAtMax
            );

        var ordersRootObject = new OrdersRootObject
        {
            Orders = ordersDto
        };

        return OkResult(ordersRootObject, parameters.Fields);
    }

    /// <summary>
    ///     Receive a list of all Orders
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("seller", Name = "GetOrdersBySellerId")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetOrdersBySellerId([FromQuery] OrdersParametersModel parameters)
    {
        var seller = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (seller is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        //if (!await CheckPermissions(seller.Id))
        //{
        //    return AccessDenied();
        //}

        if (parameters.Page < Constants.Configurations.DefaultPageValue)
        {
            return Error(HttpStatusCode.BadRequest, "page", "Invalid page parameter");
        }

        if (parameters.Limit < Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
        {
            return Error(HttpStatusCode.BadRequest, "limit", "Invalid limit parameter");
        }

        var storeId = _storeContext.GetCurrentStore().Id;

        IList<OrderDto> ordersDto = await _orderApiService.GetOrders(
                customerId: parameters.CustomerId,
                limit: parameters.Limit,
                page: parameters.Page,
                status: parameters.Status,
                paymentStatus: parameters.PaymentStatus,
                shippingStatus: parameters.ShippingStatus,
                storeId: storeId,
                orderByDateDesc: parameters.OrderByDateDesc,
                createdAtMin: parameters.CreatedAtMin,
                createdAtMax: parameters.CreatedAtMax,
                sellerId: seller.Id
            );

        var ordersRootObject = new OrdersRootObject
        {
            Orders = ordersDto
        };

        return OkResult(ordersRootObject, parameters.Fields);
    }

    /// <summary>
    ///     Receive a list of all Orders
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("syncdata", Name = "SyncOrders")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> SyncData(long? lastUpdateTs, string? fields)
    {
        var seller = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (seller is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var storeId = _storeContext.GetCurrentStore().Id;

        DateTime? lastUpdateUtc = null;

        if (lastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(lastUpdateTs.Value);
        }

        IList<OrderDto> ordersDto = await _orderApiService.GetLastestUpdatedItemsAsync(lastUpdateUtc, seller.Id, storeId);

        var ordersRootObject = new OrdersRootObject
        {
            Orders = ordersDto
        };

        return OkResult(ordersRootObject, fields);
    }

    /// <summary>
    ///     Receive a list of all Orders
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("syncdata2", Name = "SyncOrders2")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(BaseSyncResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> SyncData2(Sync2ParametersModel body)
    {
        var seller = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (seller is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var storeId = _storeContext.GetCurrentStore().Id;

        DateTime? lastUpdateUtc = null;

        if (body.LastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(body.LastUpdateTs.Value);
        }

        var result = await _orderApiService.GetLastestUpdatedItems2Async(body.IdsInDb, lastUpdateUtc, seller.Id);

        return Ok(result);
    }


    /// <summary>
    ///     Place an order
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost(Name = "PlaceOrder")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> PlaceOrder(OrderPost newOrderPost)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id))
        {
            return AccessDenied();
        }

        int billingAddressId = newOrderPost.BillingAddressId ?? 0;

        if (billingAddressId == 0)
        {
            return Error(HttpStatusCode.BadRequest, "billingAddress", "non-existing billing address");
        }

        var addressValidation = await _customerApiService.GetCustomerAddressAsync(customer.Id, billingAddressId);

        if (addressValidation is null)
        {
            return Error(HttpStatusCode.BadRequest, "billingAddress", "the address does not belong to client");
        }

        List<ShoppingCartItem> cart = await _shoppingCartItemApiService.GetShoppingCartItemsAsync(customerId: customer.Id, shoppingCartType: ShoppingCartType.ShoppingCart);

        if (!cart.Any())
        {
            return Error(HttpStatusCode.BadRequest, "cart", "the customer cart is empty");
        }

        customer.BillingAddressId = billingAddressId;
        customer.ShippingAddressId = billingAddressId;

        await CustomerService.UpdateCustomerAsync(customer); // update billing and shipping addresses

        int storeId = _storeContext.GetCurrentStore().Id;

        //Empty cart
        await _shoppingCartItemApiService.EmptyCartAsync(customerId: customer.Id, shoppingCartType: ShoppingCartType.ShoppingCart);

        OrdersIdRootObject ordersIdRootObject = new();

        while (cart.Any())
        {
            //Get a cart section
            List<ShoppingCartItem> cartSection = cart.Take(_orderSettings.MaxItemsPerOrder).ToList();

            cart.RemoveAll(item => cartSection.Any(item2Delete => item.Id == item2Delete.Id));

            await _shoppingCartItemApiService.AddShoppingCartItemsToCartAsync(cartSection);

            var placeOrderResult = await _orderApiService.PlaceOrderAsync(newOrderPost, customer, storeId, cartSection);

            if (!placeOrderResult.Success)
            {
                foreach (var error in placeOrderResult.Errors)
                {
                    ModelState.AddModelError("order_placement", error);
                }

                return Error(HttpStatusCode.BadRequest);
            }

            await CustomerActivityService.InsertActivityAsync("AddNewOrder", await LocalizationService.GetResourceAsync("ActivityLog.AddNewOrder"), placeOrderResult.PlacedOrder);

            ordersIdRootObject.Orders.Add(placeOrderResult.PlacedOrder.Id);
        }

        return OkResult(ordersIdRootObject);
    }

    /// <summary>
    ///     Place an order
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("seller/{customerId}", Name = "PlaceOrderWithCustomer")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> PlaceOrderWithCustomer([FromBody] OrderPost newOrderPost, [FromRoute] int customerId)
    {
        var seller = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (seller is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var customer = await _customerApiService.GetCustomerEntityByIdAsync(customerId);

        if (customer is null)
        {
            return Error(HttpStatusCode.BadRequest, "customer_id", "non-existing customer");
        }

        if (!IsCustomerRelatedToSeller(seller, customer))
        {
            return Error(HttpStatusCode.BadRequest, "customer_id", "the customer is not related to this seller");
        }

        int billingAddressId = newOrderPost.BillingAddressId ?? 0;

        if (billingAddressId == 0)
        {
            return Error(HttpStatusCode.BadRequest, "billingAddress", "non-existing billing address");
        }

        var addressValidation = await _customerApiService.GetCustomerAddressAsync(customer.Id, billingAddressId);

        if (addressValidation is null)
        {
            return Error(HttpStatusCode.BadRequest, "billingAddress", "the address does not belong to client");
        }

        if (newOrderPost.OrderItems is null)
        {
            return Error(HttpStatusCode.BadRequest, "order_items", "the order_items field cannot be null");
        }

        var store = _storeContext.GetCurrentStore();

        var warnings = await _shoppingCartItemApiService.ReplaceCartAsync(customer, newOrderPost.OrderItems, store.Id);

        var error = GetWarningBatchErrors(warnings);
        if (error is not null) return error;


        List<ShoppingCartItem> cart = await _shoppingCartItemApiService.GetShoppingCartItemsAsync(customerId: customer.Id, shoppingCartType: ShoppingCartType.ShoppingCart);


        customer.BillingAddressId = billingAddressId;
        customer.ShippingAddressId = billingAddressId;

        await CustomerService.UpdateCustomerAsync(customer); // update billing and shipping addresses

        int storeId = _storeContext.GetCurrentStore().Id;

        //Empty cart
        await _shoppingCartItemApiService.EmptyCartAsync(customerId: customer.Id, shoppingCartType: ShoppingCartType.ShoppingCart);

        OrdersIdRootObject ordersIdRootObject = new();

        while (cart.Any())
        {
            //Get a cart section
            List<ShoppingCartItem> cartSection = cart.Take(_orderSettings.MaxItemsPerOrder).ToList();

            cart.RemoveAll(item => cartSection.Any(item2Delete => item.Id == item2Delete.Id));

            await _shoppingCartItemApiService.AddShoppingCartItemsToCartAsync(cartSection);

            var placeOrderResult = await _orderApiService.PlaceOrderAsync(newOrderPost, customer, storeId, cartSection);

            if (!placeOrderResult.Success)
            {
                foreach (var placeOrdererror in placeOrderResult.Errors)
                {
                    ModelState.AddModelError("order_placement", placeOrdererror);
                }

                return Error(HttpStatusCode.BadRequest);
            }

            await CustomerActivityService.InsertActivityAsync("AddNewOrder", await LocalizationService.GetResourceAsync("ActivityLog.AddNewOrder"), placeOrderResult.PlacedOrder);

            ordersIdRootObject.Orders.Add(placeOrderResult.PlacedOrder.Id);
        }

        return OkResult(ordersIdRootObject);
    }


    /// <summary>
    ///     Place an order
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("seller/batch/{customerId}", Name = "PlaceManyOrdersWithCustomer")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> PlaceManyOrdersWithCustomer([FromBody] List<OrderPost> newOrderPostList, [FromRoute] int customerId)
    {
        var seller = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (seller is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var customer = await _customerApiService.GetCustomerEntityByIdAsync(customerId);

        if (customer is null)
        {
            return Error(HttpStatusCode.BadRequest, "customer_id", "non-existing customer");
        }

        if (!IsCustomerRelatedToSeller(seller, customer))
        {
            return Error(HttpStatusCode.BadRequest, "customer_id", "the customer is not related to this seller");
        }

        if (newOrderPostList is null || newOrderPostList.Count == 0)
        {
            return Error(HttpStatusCode.BadRequest, "order_manager", "order_manager is empty");
        }

        OrdersIdRootObject ordersIdRootObject = new();

        //int billingAddressId = newOrderPostList.FirstOrDefault()?.BillingAddressId ?? 0;

        //if (billingAddressId == 0)
        //{
        //    return Error(HttpStatusCode.BadRequest, "billingAddress", "non-existing billing address");
        //}

        //var addressValidation = await _customerApiService.GetCustomerAddressAsync(customer.Id, billingAddressId);

        //if (addressValidation is null)
        //{
        //    return Error(HttpStatusCode.BadRequest, "billingAddress", "the address does not belong to client");
        //}

        var customersWithAddresses = await _customerApiService.JoinCustomersWithAddressesAsync(new List<Customer> { customer });

        var address = customersWithAddresses.FirstOrDefault()?.Addresses.FirstOrDefault();

        if (address is null)
        {
            return Error(HttpStatusCode.BadRequest, "billingAddress", "the customer does not have a billing address");
        }

        customer.BillingAddressId = address.Id;
        customer.ShippingAddressId = address.Id;

        await CustomerService.UpdateCustomerAsync(customer); // update billing and shipping addresses

        int storeId = _storeContext.GetCurrentStore().Id;

        int index = 0;

        foreach (var newOrderPost in newOrderPostList)
        {
            if (newOrderPost.OrderItems is null)
            {
                return Error(HttpStatusCode.BadRequest, $"order: {index} - order_items", "the order_items field cannot be null");
            }

            var store = _storeContext.GetCurrentStore();

            var warnings = await _shoppingCartItemApiService.ReplaceCartAsync(customer, newOrderPost.OrderItems, store.Id);

            var error = GetWarningBatchErrors(warnings);
            if (error is not null) return error;

            //List<ShoppingCartItem> cartSection = await _shoppingCartItemApiService.GetShoppingCartItemsAsync(customerId: customer.Id, shoppingCartType: ShoppingCartType.ShoppingCart);

            var cartSection = newOrderPost.OrderItems.Select(item => new ShoppingCartItem
            {
                CustomerId = customer.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                ShoppingCartType = item.ShoppingCartType,
            }).ToList();

            var placeOrderResult = await _orderApiService.PlaceOrderAsync(newOrderPost, customer, storeId, cartSection);

            if (!placeOrderResult.Success)
            {
                foreach (var placeOrdererror in placeOrderResult.Errors)
                {
                    ModelState.AddModelError("order_placement", placeOrdererror);
                }

                return Error(HttpStatusCode.BadRequest);
            }

            await CustomerActivityService.InsertActivityAsync("AddNewOrder", await LocalizationService.GetResourceAsync("ActivityLog.AddNewOrder"), placeOrderResult.PlacedOrder);

            ordersIdRootObject.Orders.Add(placeOrderResult.PlacedOrder.Id);

            index++;
        }

        return OkResult(ordersIdRootObject);
    }


    /// <summary>
    ///     Place an order
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("seller/batch2", Name = "PlaceManyOrders2")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(List<int>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> PlaceManyOrders2([FromBody] List<OrderPost2> orderPostList, [FromQuery] int customerId, [FromQuery] int billingAddressId, [FromQuery] Guid orderManagerGuid)
    {
        var seller = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (seller is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var customer = await _customerApiService.GetCustomerEntityByIdAsync(customerId);

        if (customer is null)
        {
            return Error(HttpStatusCode.BadRequest, "customer_id", "non-existing customer");
        }

        if (!IsCustomerRelatedToSeller(seller, customer))
        {
            return Error(HttpStatusCode.BadRequest, "customer_id", "the customer is not related to this seller");
        }

        if (orderPostList is null || orderPostList.Count == 0)
        {
            return Error(HttpStatusCode.BadRequest, "order_manager", "order_manager is empty");
        }

        var store = _storeContext.GetCurrentStore();

        var result = await _orderApiService.PlaceManyOrderAsync(customer, billingAddressId, orderManagerGuid, orderPostList, store.Id);

        if (!result.Success)
        {
            foreach (var placeOrderError in result.Errors)
            {
                ModelState.AddModelError("order_placement", placeOrderError);
            }

            return Error(HttpStatusCode.BadRequest);
        }

        var ordersId = result.PlacedOrders.Select(order => order.Id).ToList();

        return OkResult(ordersId);
    }


    #endregion

    #region Private methods
    //private OrderSettings OrderSettings => _orderSettings ?? (_orderSettings = EngineContext.Current.Resolve<OrderSettings>());

    private bool IsCustomerRelatedToSeller(Customer seller, Customer customer)
    {
        return customer.SellerId == seller.Id;
    }

    private async Task<bool> CheckPermissions(int? customerId)
    {
        var currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (currentCustomer is null) // authenticated, but does not exist in db
            return false;

        if (customerId.HasValue && currentCustomer.Id == customerId)
        {
            // if I want to handle my own orders, check only public store permission
            return await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart, currentCustomer);
        }

        return false;
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

    /*
    private async Task<bool> SetShippingOptionAsync(
        string shippingRateComputationMethodSystemName, string shippingOptionName, int storeId, Customer customer, List<ShoppingCartItem> shoppingCartItems)
    {
        var isValid = true;

        if (string.IsNullOrEmpty(shippingRateComputationMethodSystemName))
        {
            isValid = false;

            ModelState.AddModelError("shipping_rate_computation_method_system_name",
                                     "Please provide shipping_rate_computation_method_system_name");
        }
        else if (string.IsNullOrEmpty(shippingOptionName))
        {
            isValid = false;

            ModelState.AddModelError("shipping_option_name", "Please provide shipping_option_name");
        }
        else
        {
            var shippingOptionResponse = await _shippingService.GetShippingOptionsAsync(shoppingCartItems, await CustomerService.GetCustomerShippingAddressAsync(customer), customer,
                                                                             shippingRateComputationMethodSystemName, storeId);

            if (shippingOptionResponse.Success)
            {
                var shippingOptions = shippingOptionResponse.ShippingOptions.ToList();

                var shippingOption = shippingOptions
                    .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(shippingOptionName, StringComparison.InvariantCultureIgnoreCase));

                await _genericAttributeService.SaveAttributeAsync(customer,
                                                       NopCustomerDefaults.SelectedShippingOptionAttribute,
                                                       shippingOption, storeId);
            }
            else
            {
                isValid = false;

                foreach (var errorMessage in shippingOptionResponse.Errors)
                {
                    ModelState.AddModelError("shipping_option", errorMessage);
                }
            }
        }

        return isValid;
    }

    private async Task<bool> IsShippingAddressRequiredAsync(ICollection<OrderItemDto> orderItems)
    {
        var shippingAddressRequired = false;

        foreach (var orderItem in orderItems)
        {
            if (orderItem.ProductId != null)
            {
                var product = await _productService.GetProductByIdAsync(orderItem.ProductId.Value);

                shippingAddressRequired |= product.IsShipEnabled;
            }
        }

        return shippingAddressRequired;
    }

    */

    #endregion
}
