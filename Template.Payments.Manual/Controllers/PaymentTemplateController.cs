using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Template.Payments.Manual.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Template.Payments.Manual.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.Admin)]
[AutoValidateAntiforgeryToken]
public class PaymentTemplateController<T, USettings> : BasePaymentController where T : ITemplateConfigurationModel, new() where USettings : TemplatePaymentSettings, new()
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly TemplateDescriptorUtility TemplateDescriptorUtility;

    #endregion

    #region Ctor

    public PaymentTemplateController(
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext,
        TemplateDescriptorUtility templateDescriptorUtility)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
        TemplateDescriptorUtility = templateDescriptorUtility;
    }

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var templatePaymentSettings = await _settingService.LoadSettingAsync<USettings>(storeScope);

        var model = new T
        {
            DescriptionText = templatePaymentSettings.DescriptionText,
            AdditionalFee = templatePaymentSettings.AdditionalFee,
            AdditionalFeePercentage = templatePaymentSettings.AdditionalFeePercentage,
            ShippableProductRequired = templatePaymentSettings.ShippableProductRequired,
            SkipPaymentInfo = templatePaymentSettings.SkipPaymentInfo,
            ActiveStoreScopeConfiguration = storeScope,
            ControllerName = TemplateDescriptorUtility.AddressName
        };

        if (storeScope > 0)
        {
            model.DescriptionText_OverrideForStore = await _settingService.SettingExistsAsync(templatePaymentSettings, x => x.DescriptionText, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(templatePaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(templatePaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            model.ShippableProductRequired_OverrideForStore = await _settingService.SettingExistsAsync(templatePaymentSettings, x => x.ShippableProductRequired, storeScope);
            model.SkipPaymentInfo_OverrideForStore = await _settingService.SettingExistsAsync(templatePaymentSettings, x => x.SkipPaymentInfo, storeScope);
        }

        return View($"~/Plugins/{TemplateDescriptorUtility.SystemName}/Views/Configure.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(T model)
    {
        System.Console.WriteLine(model.DescriptionText);


        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
        {
            System.Console.WriteLine("==============MODEL IS INVALID================");
            string json = JsonSerializer.Serialize(ModelState);
            Console.WriteLine(json);
            _notificationService.ErrorNotification("No se pudieron guardar las actualizaciones, consulte asistencia técnica");
            return await Configure();
        }

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var templatePaymentSettings = await _settingService.LoadSettingAsync<USettings>(storeScope);

        //save settings
        templatePaymentSettings.DescriptionText = model.DescriptionText;
        templatePaymentSettings.AdditionalFee = model.AdditionalFee;
        templatePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        templatePaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
        templatePaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

        /* We do not clear cache after each setting update.
         * This behavior can increase performance because cached settings will not be cleared 
         * and loaded from database after each update */
        await _settingService.SaveSettingOverridablePerStoreAsync(templatePaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(templatePaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(templatePaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(templatePaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(templatePaymentSettings, x => x.SkipPaymentInfo, model.SkipPaymentInfo_OverrideForStore, storeScope, false);

        //now clear settings cache
        await _settingService.ClearCacheAsync();

        //localization. no multi-store support for localization yet.
        //foreach (var localized in model.Locales)
        //{
        //    await _localizationService.SaveLocalizedSettingAsync(templatePaymentSettings, x => x.DescriptionText,
        //        localized.LanguageId,
        //        localized.DescriptionText);
        //}

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}