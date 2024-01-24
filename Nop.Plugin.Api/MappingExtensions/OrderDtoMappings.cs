using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTO.OrderItems;
using Nop.Plugin.Api.DTO.Orders;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Api.MappingExtensions;

#nullable enable

public static class OrderDtoMappings
{
    public static OrderDto ToDto(this Order order, IList<OrderItem> orderItems, Address address, Dictionary<string, object> CustomValues, Customer? customer)
    {
        var OrderDto = order.MapTo<Order, OrderDto>();

        var orderItemsDto = orderItems.Select(x => x.ToDto()).ToList();

        var addressDto = address.ToDto();

        OrderDto.OrderItems = orderItemsDto;
        OrderDto.BillingAddress = addressDto;
        OrderDto.CustomValues = CustomValues;
        OrderDto.Customer = customer?.ToDto();

        return OrderDto;
    }
}
