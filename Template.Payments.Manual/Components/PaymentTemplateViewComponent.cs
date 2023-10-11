using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Template.Payments.Manual.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Framework.Components;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Vendors;
using Nop.Core.Http.Extensions;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Media;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Framework.Models;

namespace Template.Payments.Manual.Components;

//[ViewComponent(Name = TemplateDefaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
public class PaymentTemplateViewComponent<T, USettings> : NopViewComponent where T : ITemplatePaymentInfoModel , new() where USettings : TemplatePaymentSettings, new()
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly TemplateDescriptorUtility TemplateDescriptorUtility;


    #endregion

    #region Ctor

    public PaymentTemplateViewComponent(
        ILocalizationService localizationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        TemplateDescriptorUtility templateDescriptorUtility
    )
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        TemplateDescriptorUtility = templateDescriptorUtility;
    }

    #endregion

    #region Methods

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentStore = await _storeContext.GetCurrentStoreAsync();
        var currentLanguage = await _workContext.GetWorkingLanguageAsync();
        var templatePaymentSettings = await _settingService.LoadSettingAsync<USettings>(currentStore.Id);

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, store.Id);

        var orderTotals = await GetOrderTotals(cart);

        var model = new T
        {
            DescriptionText = await _localizationService.GetLocalizedSettingAsync(templatePaymentSettings, x => x.DescriptionText, currentLanguage.Id, 0),
            OrderTotals = orderTotals,
        };

        return View($"~/Plugins/{TemplateDescriptorUtility.SystemName}/Views/PaymentInfo.cshtml", (ITemplatePaymentInfoModel)model);
    }

    private async Task<Dictionary<string, string>?> GetOrderTotals(IList<ShoppingCartItem> cart)
    {
        Dictionary<string, string>? orderTotals = null;

        if (cart.Any())
        {
            orderTotals = new();

            //total
            var (shoppingCartTotalBase, _, _, _, _, _) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart);

            if (shoppingCartTotalBase != null)
            {
                var currencies = await _currencyService.GetAllCurrenciesAsync();

                foreach (var item in currencies)
                {
                    var name = item.Name;
                    var amount = (shoppingCartTotalBase ?? 0) * item.Rate;
                    var priceFormated = await _priceFormatter.FormatPriceAsync(amount, true, item);

                    orderTotals.Add(name, priceFormated);
                }
            }
        }

        return orderTotals;
    }

    #endregion
}
