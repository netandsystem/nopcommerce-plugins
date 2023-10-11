using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template.Payments.Manual;

namespace Nas.Nop.Plugin.Payments.PagoMovil;

public static class DefaultDescriptor
{
    public const string SystemName = "Payments.PagoMovil";
    public const string AddressName = "PaymentsPagoMovil";
    public static TemplateDescriptorUtility TemplateDescriptorUtility { get; } = new(SystemName, "Pago Movil");
}
