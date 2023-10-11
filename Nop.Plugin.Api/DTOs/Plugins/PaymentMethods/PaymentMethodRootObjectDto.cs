using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Plugin.Api.DTO;

namespace Nop.Plugin.Api.DTOs.Plugins.PaymentMethods;
public class PaymentMethodRootObjectDto : ISerializableObject
{
    [JsonProperty("payment_methods")]
    public IList<PaymentMethodDto> PaymentMethods { get; set; }

    public PaymentMethodRootObjectDto()
    {
        PaymentMethods = new List<PaymentMethodDto>();
    }

    public string GetPrimaryPropertyName()
    {
        return "payment_methods";
    }

    public Type GetPrimaryPropertyType()
    {
        return typeof(PaymentMethodDto);
    }
}
