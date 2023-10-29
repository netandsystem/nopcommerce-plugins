using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Plugin.Api.DTO;

namespace Nop.Plugin.Api.DTOs.ShippingMethod;
public class ShippingMethodRootObjectDto : ISerializableObject
{
    [JsonProperty("shipping_methods")]
    public IList<ShippingMethodDto> ShippingMethods { get; set; }

    public ShippingMethodRootObjectDto()
    {
        ShippingMethods = new List<ShippingMethodDto>();
    }

    public string GetPrimaryPropertyName()
    {
        return "shipping_methods";
    }

    public Type GetPrimaryPropertyType()
    {
        return typeof(ShippingMethodDto);
    }
}
