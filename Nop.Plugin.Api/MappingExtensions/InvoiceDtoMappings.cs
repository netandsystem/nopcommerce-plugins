using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTOs.Orders;

#nullable enable

namespace Nop.Plugin.Api.MappingExtensions;

public static class InvoiceDtoMappings
{
    public static InvoiceDto ToDto(this Invoice item)
    {
        return item.MapTo<Invoice, InvoiceDto>();
    }
}
