using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using Template.Payments.Manual.Models;
using Nas.Nop.Plugin.Payments.PagoMovil;

namespace NaS.Nop.Plugin.Payments.PagoMovil.Models;

public record PaymentInfoModel : BaseNopModel, ITemplatePaymentInfoModel
{
    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.Fields.OperationNumber")]
    public string? OperationNumber { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.Fields.OrderTotals")]
    public Dictionary<string, string>? OrderTotals { get; set; }

    [NopResourceDisplayName($"Plugins.{DefaultDescriptor.SystemName}.Fields.DescriptionText")]
    public string? DescriptionText { get; set; }
}