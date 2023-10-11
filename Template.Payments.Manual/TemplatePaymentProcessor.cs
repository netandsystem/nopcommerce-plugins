using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Template.Payments.Manual.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Template.Payments.Manual.Validators;
using Template.Payments.Manual.Models;
using System.Linq;
using Nop.Web.Framework.Models;

namespace Template.Payments.Manual;

/// <summary>
/// Template payment processor
/// </summary>
public class TemplatePaymentProcessor<T, USettings> : BasePlugin, IPaymentMethod where T : BaseNopModel, ITemplatePaymentInfoModel, new() where USettings : TemplatePaymentSettings, new()
{
    #region Fields

    private readonly USettings _templatePaymentSettings;
    private readonly ILocalizationService _localizationService;
    private readonly IOrderTotalCalculationService _orderTotalCalculationService;
    private readonly ISettingService _settingService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IWebHelper _webHelper;
    private readonly TemplateDescriptorUtility TemplateDescriptorUtility;


    #endregion

    #region Ctor

    public TemplatePaymentProcessor(
        USettings templatePaymentSettings,
        ILocalizationService localizationService,
        IOrderTotalCalculationService orderTotalCalculationService,
        ISettingService settingService,
        IShoppingCartService shoppingCartService,
        IWebHelper webHelper,
        TemplateDescriptorUtility templateDescriptorUtility
    )
    {
        _templatePaymentSettings = templatePaymentSettings;
        _localizationService = localizationService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _settingService = settingService;
        _shoppingCartService = shoppingCartService;
        _webHelper = webHelper;
        TemplateDescriptorUtility = templateDescriptorUtility;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Process a payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending });
    }

    /// <summary>
    /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
    /// </summary>
    /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
    public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {
        //nothing
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a value indicating whether payment method should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shoping cart</param>
    /// <returns>true - hide; false - display.</returns>
    public async Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        //you can put any logic here
        //for example, hide this payment method if all products in the cart are downloadable
        //or hide this payment method if current customer is from certain country
        return _templatePaymentSettings.ShippableProductRequired && !await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart);
    }

    /// <summary>
    /// Gets additional handling fee
    /// </summary>
    /// <param name="cart">Shoping cart</param>
    /// <returns>Additional handling fee</returns>
    public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return await _orderTotalCalculationService.CalculatePaymentAdditionalFeeAsync(cart,
            _templatePaymentSettings.AdditionalFee, _templatePaymentSettings.AdditionalFeePercentage);
    }

    /// <summary>
    /// Captures payment
    /// </summary>
    /// <param name="capturePaymentRequest">Capture payment request</param>
    /// <returns>Capture payment result</returns>
    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="refundPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
    }

    /// <summary>
    /// Voids a payment
    /// </summary>
    /// <param name="voidPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
    }

    /// <summary>
    /// Process recurring payment
    /// </summary>
    /// <param name="processPaymentRequest">Payment info required for an order processing</param>
    /// <returns>Process payment result</returns>
    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Cancels a recurring payment
    /// </summary>
    /// <param name="cancelPaymentRequest">Request</param>
    /// <returns>Result</returns>
    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
    }

    /// <summary>
    /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
    /// </summary>
    /// <param name="order">Order</param>
    /// <returns>Result</returns>
    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order));

        //it's not a redirection payment method. So we always return false
        return Task.FromResult(false);
    }

    /// <summary>
    /// Validate payment form
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the list of validating errors
    /// </returns>
    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {

        //return Task.FromResult<IList<string>>(new List<string>());
        var warnings = new List<string>();

        //validate
        var validator = new PaymentInfoValidator<T>(_localizationService, TemplateDescriptorUtility);

        var model = new T
        {
            OperationNumber = form["OperationNumber"].ToString(),
        };

        var validationResult = validator.Validate(model);

        if (!validationResult.IsValid)
            warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

        return Task.FromResult<IList<string>>(warnings);
    }

    /// <summary>
    /// Get payment information
    /// </summary>
    /// <param name="form">The parsed form values</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the payment info holder
    /// </returns>
    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {

        //return Task.FromResult(new ProcessPaymentRequest());
        return Task.FromResult(new ProcessPaymentRequest
        {
            CustomValues = new Dictionary<string, object>()
            {
                {"Numero de Operacion" , form["OperationNumber"].ToString().Trim()},
            }
        });
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/{TemplateDescriptorUtility.AddressName}/Configure";
    }

    public override async Task InstallAsync()
    {
        var settings = new USettings
        {
            DescriptionText = "<p style=\"text-align: left;\"><span style=\"font-size: 11pt;\"><strong>Realiza el pago por medio de <span style=\"color: #3598db;\">Banesco </span>usando los siguientes datos:</strong></span></p>\r\n<ul style=\"list-style-type: none; padding-left: 0px;\">\r\n<li class=\"text\">✅ <strong>Cuenta</strong>: 0123-4564-45654-451</li>\r\n<li>✅ <strong>Razon Social</strong>: Ferreteria Principal, PZO C.A</li>\r\n<li>✅ <strong>Banco</strong>: Banesco</li>\r\n</ul>",
            SkipPaymentInfo = false
        };

        await _settingService.SaveSettingAsync(settings);

        //locales
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.DescriptionText"] = "Intrucciones",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.DescriptionText.Hint"] = "Ingresa las instrucciones para utilizar el método de pago que los clientes verán durante la comprobación",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.AdditionalFee"] = "Tarifa adicional fija",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.AdditionalFee.Hint"] = "Tarifa fija por usar el métdo de pago.",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.AdditionalFeePercentage"] = "Tarifa adicional porcentual",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.AdditionalFeePercentage.Hint"] = "Determina si aplicar un cargo adicional en porcentaje al total del pedido. Si no está habilitado, se utiliza un valor fijo.",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.ShippableProductRequired"] = "Producto apto para envío requerido",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.ShippableProductRequired.Hint"] = "Una opción que indica si se requieren productos aptos para envío para mostrar este método de pago durante el proceso de pago.",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.PaymentMethodDescription"] = $"Paga con \"{TemplateDescriptorUtility.FriendlyName}\"",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.SkipPaymentInfo"] = "Omitir página de información de pago",
            [$"Plugins.{TemplateDescriptorUtility.AddressName}.SkipPaymentInfo.Hint"] = "Una opción que indica si debemos mostrar una página de información de pago para este complemento.",
            [$"Plugins.{TemplateDescriptorUtility.SystemName}.Fields.OperationNumber.Required"] = "El campo no puede estar vacio",
            [$"Plugins.{TemplateDescriptorUtility.SystemName}.Fields.OperationNumber.WrongLength"] = "El número debe tener 5 dígitos",
            [$"Plugins.{TemplateDescriptorUtility.SystemName}.Fields.OperationNumber.Wrong"] = "Debe ingresar solo números"
        });

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        //settings
        await _settingService.DeleteSettingAsync<USettings>();

        //locales
        await _localizationService.DeleteLocaleResourcesAsync($"Plugins.{TemplateDescriptorUtility.SystemName}");

        await base.UninstallAsync();
    }

    /// <summary>
    /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
    /// </summary>
    /// <returns>View component name</returns>
    public string GetPublicViewComponentName()
    {
        return TemplateDescriptorUtility.AddressName;
    }

    /// <summary>
    /// Gets a payment method description that will be displayed on checkout pages in the public store
    /// </summary>
    /// <remarks>
    /// return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
    /// for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync($"Plugins.{TemplateDescriptorUtility.SystemName}.PaymentMethodDescription");
    }

    #endregion

    #region Properies

    /// <summary>
    /// Gets a value indicating whether capture is supported
    /// </summary>
    public bool SupportCapture => false;

    /// <summary>
    /// Gets a value indicating whether partial refund is supported
    /// </summary>
    public bool SupportPartiallyRefund => false;

    /// <summary>
    /// Gets a value indicating whether refund is supported
    /// </summary>
    public bool SupportRefund => false;

    /// <summary>
    /// Gets a value indicating whether void is supported
    /// </summary>
    public bool SupportVoid => false;

    /// <summary>
    /// Gets a recurring payment type of payment method
    /// </summary>
    public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

    /// <summary>
    /// Gets a payment method type
    /// </summary>
    public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

    /// <summary>
    /// Gets a value indicating whether we should display a payment information page for this plugin
    /// </summary>
    public bool SkipPaymentInfo => _templatePaymentSettings.SkipPaymentInfo;

    #endregion
}
