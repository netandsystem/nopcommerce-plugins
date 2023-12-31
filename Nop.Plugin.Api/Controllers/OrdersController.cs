﻿using System;
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
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.DTO.ShoppingCarts;
using Nop.Plugin.Api.DTOs.ShoppingCarts;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentMigrator.Runner.Processors.Firebird;
using Nop.Core.Domain.Stores;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Policies;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/orders")]
[Authorize(Policy = CustomerRoleAuthorizationPolicy.Name)]
public class OrdersController : BaseApiController
{
    #region Fields

    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IOrderApiService _orderApiService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IOrderService _orderService;
    private readonly IProductAttributeConverter _productAttributeConverter;
    private readonly IPaymentService _paymentService;
    private readonly IPdfService _pdfService;
    private readonly IPermissionService _permissionService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IProductService _productService;
    private readonly IShippingService _shippingService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IStoreContext _storeContext;
    private readonly ITaxPluginManager _taxPluginManager;
    private readonly IShoppingCartItemApiService _shoppingCartItemApiService;
    private readonly ICustomerApiService _customerApiService;


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
        ICustomerApiService customerApiService)
        : base(jsonFieldsSerializer, aclService, customerService, storeMappingService,
               storeService, discountService, customerActivityService, localizationService, pictureService)
    {
        _orderApiService = orderApiService;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _shoppingCartService = shoppingCartService;
        _genericAttributeService = genericAttributeService;
        _storeContext = storeContext;
        _shippingService = shippingService;
        _productService = productService;
        _productAttributeConverter = productAttributeConverter;
        _paymentService = paymentService;
        _pdfService = pdfService;
        _permissionService = permissionService;
        _authenticationService = authenticationService;
        _taxPluginManager = taxPluginManager;
        _shoppingCartItemApiService = shoppingCartItemApiService;
        _customerApiService = customerApiService;
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
                customer.Id,
                parameters.Limit,
                parameters.Page,
                parameters.Status,
                parameters.PaymentStatus,
                parameters.ShippingStatus,
                storeId,
                parameters.OrderByDateDesc,
                parameters.CreatedAtMin,
                parameters.CreatedAtMax
            );

        var ordersRootObject = new OrdersRootObject
        {
            Orders = ordersDto
        };

        return OkResult( ordersRootObject, parameters.Fields);
    }

    /// <summary>
    ///     Place an order
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost(Name = "PlaceOrder")]
    [ProducesResponseType(typeof(OrdersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> PlaceOrder( OrderPost newOrderPost )
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

        while(cart.Any())
        {
            //Get a cart section
            List<ShoppingCartItem> cartSection = cart.Take(3).ToList();

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

    #endregion

    #region Private methods
    //private OrderSettings OrderSettings => _orderSettings ?? (_orderSettings = EngineContext.Current.Resolve<OrderSettings>());

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
