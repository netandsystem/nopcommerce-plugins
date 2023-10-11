using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.Banesco.Models;

public record PaymentInfoModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Payments.Banesco.Fields.OperationNumber")]
    public string? OperationNumber { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Banesco.Fields.BsAmount")]
    public Dictionary<string, string>? OrderTotals { get; set; }

    [NopResourceDisplayName("Plugins.Payments.Banesco.Fields.DescriptionText")]
    public string? DescriptionText { get; set; }
}