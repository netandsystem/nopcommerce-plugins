using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using NaS.Nop.Plugin.Payments.PagoMovil.Models;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nas.Nop.Plugin.Payments.PagoMovil;
using Template.Payments.Manual.Components;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Template.Payments.Manual;
using Template.Payments.Manual.Controllers;

namespace NaS.Nop.Plugin.Payments.PagoMovil.Controllers;

public class PaymentsPagoMovilController : PaymentTemplateController<ConfigurationModel, PagoMovilPaymentSettings>
{
    #region Fields

    #endregion

    #region Ctor

    public PaymentsPagoMovilController(
        ILocalizationService localizationService, 
        INotificationService notificationService, 
        IPermissionService permissionService, 
        ISettingService settingService, 
        IStoreContext storeContext
        ) : base(
            localizationService, 
            notificationService, 
            permissionService, 
            settingService, 
            storeContext, 
            DefaultDescriptor.TemplateDescriptorUtility
    )
    {
    }

    #endregion

    #region Methods

    #endregion
    
}