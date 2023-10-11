using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Template.Payments.Manual.Models;

public interface ITemplateConfigurationModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    //[NopResourceDisplayName("Plugins.Payment.Template.AdditionalFee")]
    public decimal AdditionalFee { get; set; }

    public bool AdditionalFee_OverrideForStore { get; set; }

    //[NopResourceDisplayName("Plugins.Payment.Template.AdditionalFeePercentage")]
    public bool AdditionalFeePercentage { get; set; }

    public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    //[NopResourceDisplayName("Plugins.Payment.Template.DescriptionText")]
    public string? DescriptionText { get; set; }

    public bool DescriptionText_OverrideForStore { get; set; }


    //[NopResourceDisplayName("Plugins.Payment.Template.ShippableProductRequired")]
    public bool ShippableProductRequired { get; set; }

    public bool ShippableProductRequired_OverrideForStore { get; set; }

    //[NopResourceDisplayName("Plugins.Payment.Template.SkipPaymentInfo")]
    public bool SkipPaymentInfo { get; set; }

    public bool SkipPaymentInfo_OverrideForStore { get; set; }
    public string? ControllerName { get; set; }
}