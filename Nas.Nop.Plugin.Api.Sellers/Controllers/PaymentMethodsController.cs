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
using Nop.Plugin.Api.DTOs.Plugins.PaymentMethods;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Core.Domain.Configuration;
using Nop.Data;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/payment_methods")]

public class PaymentMethodsController : BaseApiController
{
    #region Attributes

    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IRepository<Setting> _settingRepository;
    private const string DescriptionTextSettingSuffix = "paymentsettings.descriptiontext";

    #endregion

    #region Ctr
    public PaymentMethodsController(
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IPaymentPluginManager paymentPluginManager,
        IRepository<Setting> settingRepository
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
        _paymentPluginManager = paymentPluginManager;
        _settingRepository = settingRepository;
    }


    #endregion

    #region Methods

    /// <summary>
    ///     Receive a list of all Payment Methods
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetPaymentMethods")]
    [ProducesResponseType(typeof(PaymentMethodRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetPaymentMethods(
        string? fields
    )
    {
        var paymentMethods = await _paymentPluginManager.LoadActivePluginsAsync();

        var paymentMethodsDto = new List<PaymentMethodDto>();

        foreach (var pm in paymentMethods)
        {
            var pmDto = new PaymentMethodDto()
            {
                Id = pm.PluginDescriptor.DisplayOrder,
                SystemName = pm.PluginDescriptor.SystemName,
                FriendlyName = pm.PluginDescriptor.FriendlyName,
                Image = await _paymentPluginManager.GetPluginLogoUrlAsync(pm)
            };

            paymentMethodsDto.Add(pmDto);
        }

        var settings = await GetAllTemplateManualSettingsAsync();

        var query = from pm in paymentMethodsDto
                    join setting in settings
                    on GetDescriptionTextSettingNameByPaymentMethod(pm) equals setting.Name
                    select new PaymentMethodDto()
                    {
                        Id = pm.Id,
                        SystemName = pm.SystemName,
                        FriendlyName = pm.FriendlyName,
                        Image = pm.Image,
                        Description = setting.Value
                    };

        var result = await query.ToListAsync();

        var paymentMethodRootObjectDto = new PaymentMethodRootObjectDto
        {
            PaymentMethods = result
        };

        return OkResult(paymentMethodRootObjectDto, fields);
    }

    #endregion

    #region private Methods

    private async Task<IList<Setting>> GetAllTemplateManualSettingsAsync()
    {
        string keywords = DescriptionTextSettingSuffix;

        var settings = await _settingRepository.GetAllAsync(query =>
        {
            return from s in query
                   where s.Name.Contains(keywords)
                   select s;
        }, cache => default);

        return settings;
    }

    private string GetDescriptionTextSettingNameByPaymentMethod(PaymentMethodDto paymentMethod)
    {
        if (paymentMethod == null)
        {
            return string.Empty;
        }

        return GetPaymentMethodSimpleName(paymentMethod) + DescriptionTextSettingSuffix;
    }

    private string GetPaymentMethodSimpleName(PaymentMethodDto paymentMethod)
    {
        if (paymentMethod == null)
        {
            return string.Empty;
        }

        string paymentMethodSimpleName = paymentMethod.SystemName.Split('.')[1].ToLower();

        return paymentMethodSimpleName;
    }

    #endregion
}
