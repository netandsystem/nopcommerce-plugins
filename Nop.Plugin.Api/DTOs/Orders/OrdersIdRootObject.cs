using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Api.DTO.Orders;

public class OrdersIdRootObject : ISerializableObject
{
    public OrdersIdRootObject()
    {
        Orders = new List<int>();
    }

    [JsonProperty("orders")]
    public List<int> Orders { get; set; }

    public string GetPrimaryPropertyName()
    {
        return "orders";
    }

    public Type GetPrimaryPropertyType()
    {
        return typeof(OrderDto);
    }
}
