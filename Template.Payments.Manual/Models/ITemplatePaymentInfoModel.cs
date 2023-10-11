using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Template.Payments.Manual.Models;

public interface ITemplatePaymentInfoModel
{
    //[NopResourceDisplayName("Plugins.Payments.Template.Fields.OperationNumber")]
    public string? OperationNumber { get; set; }

    //[NopResourceDisplayName("Plugins.Payments.Template.Fields.OrderTotals")]
    public Dictionary<string, string>? OrderTotals { get; set; }

    //[NopResourceDisplayName("Plugins.Payments.Template.Fields.DescriptionText")]
    public string? DescriptionText { get; set; }
}