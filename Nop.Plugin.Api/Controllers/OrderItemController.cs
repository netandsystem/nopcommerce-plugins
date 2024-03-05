using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using System.Threading.Tasks;
using Nop.Services.Catalog;
using Nop.Plugin.Api.Infrastructure;
using Nop.Core;
using System.Net;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Services.Security;
using Nop.Services.Customers;
using Nop.Services.Stores;
using Nop.Services.Discounts;
using Nop.Services.Logging;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Models.ProductsParameters;
using Nop.Plugin.Api.Services;
using Nop.Services.Seo;
using Nop.Services.Orders;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using Nop.Services.Plugins;
using Nop.Core.Domain.Configuration;
using Nop.Data;
using Nop.Services.Shipping;
using Nop.Plugin.Api.DTOs.ShippingMethod;
using Nop.Plugin.Api.MappingExtensions;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Models.Base;
using Nop.Services.Authentication;
using Nop.Plugin.Api.DTO.OrderItems;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/order-items")]

public class OrderItemController : BaseSyncController<OrderItemDto>
{
    #region Attributes

    private readonly IOrderItemApiService _orderItemApiService;

    #endregion

    #region Ctr
    public OrderItemController(
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IOrderItemApiService orderItemApiService,
        IAuthenticationService authenticationService,
        IStoreContext storeContext
    ) :
    base(
        orderItemApiService,
        jsonFieldsSerializer,
        aclService,
        customerService,
        storeMappingService,
        storeService,
        discountService,
        customerActivityService,
        localizationService,
        pictureService,
        authenticationService,
        storeContext
    )
    {
        _orderItemApiService = orderItemApiService;
    }


    #endregion

    #region Methods

    /// <summary>
    ///     Retrieve current customer
    /// </summary>
    /// <param name="fields">Fields from the customer you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("syncdata2", Name = "SyncOrderItems2")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(BaseSyncResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    //[GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> SyncData2(Sync2ParametersModel body)
    {
        var sellerEntity = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (sellerEntity is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        DateTime? lastUpdateUtc = null;

        if (body.LastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(body.LastUpdateTs.Value);
        }

        var result = await _orderItemApiService.GetLastestUpdatedItems2Async(
                body.IdsInDb,
                lastUpdateUtc,
                sellerEntity.Id
            );

        return Ok(result);
    }

    #endregion

    #region private Methods


    #endregion
}
