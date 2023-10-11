using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Banesco.Models;
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

namespace Nop.Plugin.Payments.Banesco.Components;

[ViewComponent(Name = BanescoDefaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
public class PaymentBanescoViewComponent : NopViewComponent
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly TaxSettings _taxSettings;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly AddressSettings _addressSettings;
    private readonly CaptchaSettings _captchaSettings;
    private readonly CatalogSettings _catalogSettings;
    private readonly CommonSettings _commonSettings;
    private readonly CustomerSettings _customerSettings;
    private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
    private readonly ICheckoutAttributeParser _checkoutAttributeParser;
    private readonly ICheckoutAttributeService _checkoutAttributeService;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly IDiscountService _discountService;
    private readonly IDownloadService _downloadService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IGiftCardService _giftCardService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IPaymentService _paymentService;
    private readonly IPermissionService _permissionService;
    private readonly IPictureService _pictureService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IProductService _productService;
    private readonly IShippingService _shippingService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IStaticCacheManager _staticCacheManager;
    private readonly IStoreMappingService _storeMappingService;
    private readonly ITaxService _taxService;
    private readonly IUrlRecordService _urlRecordService;
    private readonly IVendorService _vendorService;
    private readonly IWebHelper _webHelper;
    private readonly MediaSettings _mediaSettings;
    private readonly OrderSettings _orderSettings;
    private readonly RewardPointsSettings _rewardPointsSettings;
    private readonly ShippingSettings _shippingSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly VendorSettings _vendorSettings;

    #endregion

    #region Ctor

    public PaymentBanescoViewComponent(
        ILocalizationService localizationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        TaxSettings taxSettings,
        IOrderProcessingService orderProcessingService,
        IOrderTotalCalculationService orderTotalCalculationService,
        AddressSettings addressSettings,
        CaptchaSettings captchaSettings,
        CatalogSettings catalogSettings,
        CommonSettings commonSettings,
        CustomerSettings customerSettings,
        ICheckoutAttributeFormatter checkoutAttributeFormatter,
        ICheckoutAttributeParser checkoutAttributeParser,
        ICheckoutAttributeService checkoutAttributeService,
        ICountryService countryService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IDiscountService discountService,
        IDownloadService downloadService,
        IGenericAttributeService genericAttributeService,
        IGiftCardService giftCardService,
        IHttpContextAccessor httpContextAccessor,
        IPaymentPluginManager paymentPluginManager,
        IPaymentService paymentService,
        IPermissionService permissionService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IProductAttributeFormatter productAttributeFormatter,
        IProductService productService,
        IShippingService shippingService,
        IStateProvinceService stateProvinceService,
        IStaticCacheManager staticCacheManager,
        IStoreMappingService storeMappingService,
        ITaxService taxService,
        IUrlRecordService urlRecordService,
        IVendorService vendorService,
        IWebHelper webHelper,
        MediaSettings mediaSettings,
        OrderSettings orderSettings,
        RewardPointsSettings rewardPointsSettings,
        ShippingSettings shippingSettings,
        ShoppingCartSettings shoppingCartSettings,
        VendorSettings vendorSettings
    )
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _taxSettings = taxSettings;
        _orderProcessingService = orderProcessingService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _addressSettings = addressSettings;
        _captchaSettings = captchaSettings;
        _catalogSettings = catalogSettings;
        _commonSettings = commonSettings;
        _customerSettings = customerSettings;
        _checkoutAttributeFormatter = checkoutAttributeFormatter;
        _checkoutAttributeParser = checkoutAttributeParser;
        _checkoutAttributeService = checkoutAttributeService;
        _countryService = countryService;
        _currencyService = currencyService;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _discountService = discountService;
        _downloadService = downloadService;
        _genericAttributeService = genericAttributeService;
        _giftCardService = giftCardService;
        _httpContextAccessor = httpContextAccessor;
        _paymentPluginManager = paymentPluginManager;
        _paymentService = paymentService;
        _permissionService = permissionService;
        _pictureService = pictureService;
        _priceFormatter = priceFormatter;
        _productAttributeFormatter = productAttributeFormatter;
        _productService = productService;
        _shippingService = shippingService;
        _stateProvinceService = stateProvinceService;
        _staticCacheManager = staticCacheManager;
        _storeMappingService = storeMappingService;
        _taxService = taxService;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
        _webHelper = webHelper;
        _mediaSettings = mediaSettings;
        _orderSettings = orderSettings;
        _rewardPointsSettings = rewardPointsSettings;
        _shippingSettings = shippingSettings;
        _shoppingCartSettings = shoppingCartSettings;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Methods

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentStore = await _storeContext.GetCurrentStoreAsync();
        var currentLanguage = await _workContext.GetWorkingLanguageAsync();
        var banescoPaymentSettings = await _settingService.LoadSettingAsync<BanescoPaymentSettings>(currentStore.Id);

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, store.Id);

        var orderTotals = await GetOrderTotals(cart);

        var model = new PaymentInfoModel
        {
            DescriptionText = await _localizationService.GetLocalizedSettingAsync(banescoPaymentSettings, x => x.DescriptionText, currentLanguage.Id, 0),
            OrderTotals = orderTotals,
        };

        return View("~/Plugins/Payments.Banesco/Views/PaymentInfo.cshtml", model);
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

    /*
    private class OrderTotalsModel
    {
        public bool IsEditable { get; set; }
        public string SubTotal { get; set; }
        public string SubTotalDiscount { get; set; }
        public bool RequiresShipping { get; set; }
        public string Shipping { get; set; }
        public string SelectedShippingMethod { get; set; }

        public bool HideShippingTotal { get; set; }
        public string PaymentMethodAdditionalFee { get; set; }
        public string Tax { get; set; }
        public IList<TaxRate> TaxRates { get; set; }
        public bool DisplayTaxRates { get; internal set; }
        public bool DisplayTax { get; internal set; }
        public string OrderTotal { get; internal set; }
        public string OrderTotalDiscount { get; internal set; }
    }

    private partial record TaxRate : BaseNopModel
    {
        public string Rate { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Prepare the order totals model
    /// </summary>
    /// <param name="cart">List of the shopping cart item</param>
    /// <param name="isEditable">Whether model is editable</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the order totals model
    /// </returns>
    private async Task<OrderTotalsModel> PrepareOrderTotalsModelAsync(IList<ShoppingCartItem> cart)
    {
        var model = new OrderTotalsModel
        {
            TaxRates = new List<TaxRate>()
        };

        if (cart.Any())
        {
            //subtotal
            var subTotalIncludingTax = await _workContext.GetTaxDisplayTypeAsync() == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal;
            var (orderSubTotalDiscountAmountBase, _, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, subTotalIncludingTax);
            var subtotalBase = subTotalWithoutDiscountBase;
            var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
            var subtotal = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(subtotalBase, currentCurrency);
            var currentLanguage = await _workContext.GetWorkingLanguageAsync();
            model.SubTotal = await _priceFormatter.FormatPriceAsync(subtotal, true, currentCurrency, currentLanguage.Id, subTotalIncludingTax);

            if (orderSubTotalDiscountAmountBase > decimal.Zero)
            {
                var orderSubTotalDiscountAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(orderSubTotalDiscountAmountBase, currentCurrency);
                model.SubTotalDiscount = await _priceFormatter.FormatPriceAsync(-orderSubTotalDiscountAmount, true, currentCurrency, currentLanguage.Id, subTotalIncludingTax);
            }

            //shipping info
            model.RequiresShipping = await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart);
            var customer = await _workContext.GetCurrentCustomerAsync();
            var store = await _storeContext.GetCurrentStoreAsync();
            if (model.RequiresShipping)
            {
                var shoppingCartShippingBase = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(cart);
                if (shoppingCartShippingBase.HasValue)
                {
                    var shoppingCartShipping = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartShippingBase.Value, currentCurrency);
                    model.Shipping = await _priceFormatter.FormatShippingPriceAsync(shoppingCartShipping, true);

                    //selected shipping method
                    var shippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(customer,
                        NopCustomerDefaults.SelectedShippingOptionAttribute, store.Id);
                    if (shippingOption != null)
                        model.SelectedShippingMethod = shippingOption.Name;
                }
            }
            else
            {
                model.HideShippingTotal = _shippingSettings.HideShippingTotal;
            }

            //payment method fee
            var paymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);
            var paymentMethodAdditionalFee = await _paymentService.GetAdditionalHandlingFeeAsync(cart, paymentMethodSystemName);
            var (paymentMethodAdditionalFeeWithTaxBase, _) = await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFee, customer);
            if (paymentMethodAdditionalFeeWithTaxBase > decimal.Zero)
            {
                var paymentMethodAdditionalFeeWithTax = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(paymentMethodAdditionalFeeWithTaxBase, currentCurrency);
                model.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFeeWithTax, true);
            }

            //tax
            bool displayTax;
            bool displayTaxRates;
            if (_taxSettings.HideTaxInOrderSummary && await _workContext.GetTaxDisplayTypeAsync() == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                var (shoppingCartTaxBase, taxRates) = await _orderTotalCalculationService.GetTaxTotalAsync(cart);
                var shoppingCartTax = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartTaxBase, currentCurrency);

                if (shoppingCartTaxBase == 0 && _taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    displayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
                    displayTax = !displayTaxRates;

                    model.Tax = await _priceFormatter.FormatPriceAsync(shoppingCartTax, true, false);
                    foreach (var tr in taxRates)
                    {
                        model.TaxRates.Add(new TaxRate
                        {
                            Rate = _priceFormatter.FormatTaxRate(tr.Key),
                            Value = await _priceFormatter.FormatPriceAsync(await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(tr.Value, currentCurrency), true, false),
                        });
                    }
                }
            }

            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;

            //total
            var (shoppingCartTotalBase, orderTotalDiscountAmountBase, _, _, _, _) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart);
            if (shoppingCartTotalBase.HasValue)
            {
                var shoppingCartTotal = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartTotalBase.Value, currentCurrency);
                model.OrderTotal = await _priceFormatter.FormatPriceAsync(shoppingCartTotal, true, false);
            }

            //discount
            if (orderTotalDiscountAmountBase > decimal.Zero)
            {
                var orderTotalDiscountAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(orderTotalDiscountAmountBase, currentCurrency);
                model.OrderTotalDiscount = await _priceFormatter.FormatPriceAsync(-orderTotalDiscountAmount, true, false);
            }
        }

        return model;
    }
    */

    #endregion
}
