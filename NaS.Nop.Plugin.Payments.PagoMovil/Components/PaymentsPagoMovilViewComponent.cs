using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nas.Nop.Plugin.Payments.PagoMovil;
using NaS.Nop.Plugin.Payments.PagoMovil.Models;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;
using Template.Payments.Manual;
using Template.Payments.Manual.Components;

namespace NaS.Nop.Plugin.Payments.PagoMovil.Components;

[ViewComponent(Name = DefaultDescriptor.AddressName)]
public class PaymentsPagoMovilViewComponent : PaymentTemplateViewComponent<PaymentInfoModel, PagoMovilPaymentSettings>
{
    public PaymentsPagoMovilViewComponent(ILocalizationService localizationService, ISettingService settingService, IStoreContext storeContext, IWorkContext workContext, IShoppingCartService shoppingCartService, IOrderTotalCalculationService orderTotalCalculationService, ICurrencyService currencyService, IPriceFormatter priceFormatter) : base(localizationService, settingService, storeContext, workContext, shoppingCartService, orderTotalCalculationService, currencyService, priceFormatter, DefaultDescriptor.TemplateDescriptorUtility)
    {
    }
}
