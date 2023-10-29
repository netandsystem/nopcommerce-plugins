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

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/shipping_methods")]

public class ShippingMethodsController : BaseApiController
{
    #region Attributes

    private readonly IShippingService _shippingService;

    #endregion

    #region Ctr
    public ShippingMethodsController(
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IShippingService shippingService
    ) :
    base(
        jsonFieldsSerializer,
        aclService,
        customerService,
        storeMappingService,
        storeService,
        discountService,
        customerActivityService,
        localizationService,
        pictureService
    )
    {
        _shippingService = shippingService;
    }


    #endregion

    #region Methods

    /// <summary>
    ///     Receive a list of all Shipping Methods
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetShippingMethods")]
    [ProducesResponseType(typeof(ShippingMethodRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetShippingMethods(
        string? fields
    )
    {
        var ShippingMethods = await _shippingService.GetAllShippingMethodsAsync();

        var shippingMethodsDto = ShippingMethods.Select(pm => pm.ToDto()).ToList();

        var shippingMethodRootObjectDto = new ShippingMethodRootObjectDto
        {
            ShippingMethods = shippingMethodsDto
        };

        return OkResult(shippingMethodRootObjectDto, fields);
    }

    #endregion

    #region private Methods


    #endregion
}
