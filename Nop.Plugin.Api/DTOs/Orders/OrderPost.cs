﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.OrderItems;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Api.DTO.Orders;

#nullable enable

[JsonObject(Title = "order")]
//[Validator(typeof(OrderDtoValidator))]
public class OrderPost
{
    [JsonProperty("pick_up_in_store", Required = Required.Always)]
    public bool? PickUpInStore { get; set; }

    /// <summary>
    ///     Gets or sets the payment method system name
    /// </summary>
    [JsonProperty("payment_method_system_name", Required = Required.Always)]
    public string? PaymentMethodSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the shipping method
    /// </summary>
    [JsonProperty("shipping_method", Required = Required.AllowNull)]
    public string? ShippingMethod { get; set; }

    /// <summary>
    ///     Gets or sets the shipping rate computation method identifier
    /// </summary>
    [JsonProperty("shipping_rate_computation_method_system_name", Required = Required.AllowNull)]
    public string? ShippingRateComputationMethodSystemName { get; set; }


    /// <summary>
    ///     Gets or sets the serialized CustomValues (values from ProcessPaymentRequest)
    /// </summary>
    [JsonProperty("payment_data", Required = Required.Always)]
    public PaymentData? PaymentData { get; set; }

    /// <summary>
    ///     Gets or sets the billing address
    /// </summary>
    //[JsonProperty("billing_address", Required = Required.Always)]
    [JsonProperty("billing_address_id", Required = Required.Always)]
    public int? BillingAddressId { get; set; }

}

public record PaymentData
{
    public PaymentData(string referenceNumber, string amountInBs)
    {
        ReferenceNumber = referenceNumber;
        AmountInBs = amountInBs;
    }

    [JsonProperty("reference_number", Required = Required.Always)]
    public string ReferenceNumber { get; set; }
    [JsonProperty("amount_in_bs", Required = Required.Always)]

    public string AmountInBs { get; set; }
}


/*
 
 
    /// <summary>
    ///     Gets or sets the customer currency code (at the moment of order placing)
    /// </summary>
    [JsonProperty("customer_currency_code")]
    public string CustomerCurrencyCode { get; set; }

    /// <summary>
    ///     Gets or sets the currency rate
    /// </summary>
    [JsonProperty("currency_rate")]
    public decimal? CurrencyRate { get; set; }

    /// <summary>
    ///     Gets or sets the tax rates
    /// </summary>
    [JsonProperty("tax_rates")]
    public string TaxRates { get; set; }

    /// <summary>
    ///     Gets or sets the customer language identifier
    /// </summary>
    [JsonProperty("customer_language_id")]
    public int? CustomerLanguageId { get; set; }

    /// <summary>
    ///     Gets or sets the shipping rate computation method identifier
    /// </summary>
    [JsonProperty("shipping_rate_computation_method_system_name")]
    public string ShippingRateComputationMethodSystemName { get; set; }
 
 */