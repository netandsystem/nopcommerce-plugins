using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTOs.Orders;

#nullable enable

namespace Nop.Plugin.Api.MappingExtensions;

public static class InvoiceDtoMappings
{
    public static InvoiceDto ToDto(this Invoice item, string customerExtId, string? customerName)
    {
        var dtoItem = item.MapTo<Invoice, InvoiceDto>();

        dtoItem.CustomerExtId = customerExtId;
        dtoItem.CustomerName = customerName;

        return dtoItem;
    }
}
