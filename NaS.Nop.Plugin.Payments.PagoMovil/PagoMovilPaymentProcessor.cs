using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using NaS.Nop.Plugin.Payments.PagoMovil.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Template.Payments.Manual;
using Nas.Nop.Plugin.Payments.PagoMovil;

namespace NaS.Nop.Plugin.Payments.PagoMovil;

/// <summary>
/// PagoMovil payment processor
/// </summary>
public class PagoMovilPaymentProcessor : TemplatePaymentProcessor<PaymentInfoModel, PagoMovilPaymentSettings>
{
    #region Fields

    #endregion

    #region Ctor

    public PagoMovilPaymentProcessor(
        PagoMovilPaymentSettings templatePaymentSettings,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper
    ) : base (
        templatePaymentSettings,
        localizationService,
        orderTotalCalculationService,
        settingService,
        shoppingCartService,
        webHelper,
        DefaultDescriptor.TemplateDescriptorUtility
    )
    {
    }

    #endregion

    #region Methods

    #endregion

    #region Properties

    #endregion
}