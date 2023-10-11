using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.Collections.Generic;
using Template.Payments.Manual.Models;
using Nas.Nop.Plugin.Payments.PagoMovil;

namespace NaS.Nop.Plugin.Payments.PagoMovil.Models;

public record ConfigurationModel : BaseNopModel, ITemplateConfigurationModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.AdditionalFee")]
    public decimal AdditionalFee { get; set; }

    public bool AdditionalFee_OverrideForStore { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.AdditionalFeePercentage")]
    public bool AdditionalFeePercentage { get; set; }

    public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.DescriptionText")]
    public string? DescriptionText { get; set; }

    public bool DescriptionText_OverrideForStore { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.ShippableProductRequired")]
    public bool ShippableProductRequired { get; set; }

    public bool ShippableProductRequired_OverrideForStore { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.SkipPaymentInfo")]
    public bool SkipPaymentInfo { get; set; }

    public bool SkipPaymentInfo_OverrideForStore { get; set; }

    public string? ControllerName { get; set; }
}