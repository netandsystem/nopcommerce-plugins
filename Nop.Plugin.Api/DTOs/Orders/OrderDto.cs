using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.OrderItems;
using Newtonsoft.Json.Converters;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.Helpers;

namespace Nop.Plugin.Api.DTO.Orders;

[JsonObject(Title = "order")]
//[Validator(typeof(OrderDtoValidator))]
public class OrderDto : BaseSyncDto
{
    private ICollection<OrderItemDto> _orderItems;

    /// <summary>
    /// Gets or sets the order identifier
    /// </summary>
    [JsonProperty("order_guid")]
    public Guid OrderGuid { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether a customer chose "pick up in store" shipping option
    /// </summary>
    [JsonProperty("pick_up_in_store")]
    public bool? PickUpInStore { get; set; }

    /// <summary>
    ///     Gets or sets the payment method system name
    /// </summary>
    [JsonProperty("payment_method_system_name")]
    public string PaymentMethodSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the order subtotal (incl tax)
    /// </summary>
    [JsonProperty("order_subtotal_incl_tax")]
    public decimal? OrderSubtotalInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order subtotal (excl tax)
    /// </summary>
    [JsonProperty("order_subtotal_excl_tax")]
    public decimal? OrderSubtotalExclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order subtotal discount (incl tax)
    /// </summary>
    [JsonProperty("order_sub_total_discount_incl_tax")]
    public decimal? OrderSubTotalDiscountInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order subtotal discount (excl tax)
    /// </summary>
    [JsonProperty("order_sub_total_discount_excl_tax")]
    public decimal? OrderSubTotalDiscountExclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order shipping (incl tax)
    /// </summary>
    [JsonProperty("order_shipping_incl_tax")]
    public decimal? OrderShippingInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order shipping (excl tax)
    /// </summary>
    [JsonProperty("order_shipping_excl_tax")]
    public decimal? OrderShippingExclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order tax
    /// </summary>
    [JsonProperty("order_tax")]
    public decimal? OrderTax { get; set; }

    /// <summary>
    ///     Gets or sets the order discount (applied to order total)
    /// </summary>
    [JsonProperty("order_discount")]
    public decimal? OrderDiscount { get; set; }

    /// <summary>
    ///     Gets or sets the order total
    /// </summary>
    [JsonProperty("order_total")]
    public decimal? OrderTotal { get; set; }

    /// <summary>
    ///     Gets or sets the shipping method
    /// </summary>
    [JsonProperty("shipping_method")]
    public string ShippingMethod { get; set; }

    /// <summary>
    ///     Gets or sets the shipping rate computation method identifier
    /// </summary>
    [JsonProperty("shipping_rate_computation_method_system_name")]
    public string ShippingRateComputationMethodSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the serialized CustomValues (values from ProcessPaymentRequest)
    /// </summary>
    [JsonProperty("custom_values")]
    public Dictionary<string, object> CustomValues { get; set; }

    [JsonProperty("customer_id")]
    public int? CustomerId { get; set; }

    [JsonProperty("customer")]
    public CustomerDto Customer { get; set; }

    /// <summary>
    ///     Gets or sets the billing address
    /// </summary>
    //[JsonProperty("billing_address", Required = Required.Always)]
    [JsonProperty("billing_address")]
    public AddressDto BillingAddress { get; set; }

    /// <summary>
    /// Gets or sets the associated seller
    /// </summary>
    [JsonProperty("seller_id")]
    public int? SellerId { get; set; }

    /// <summary>
    /// Gets or sets the associated order manager guid
    /// </summary>
    [JsonProperty("order_manager_guid")]
    public Guid? OrderManagerGuid { get; set; }

    /// <summary>
    ///     Gets or sets order items
    /// </summary>
    [JsonProperty("order_items")]
    public ICollection<OrderItemDto> OrderItems
    {
        get
        {
            _orderItems ??= new List<OrderItemDto>();

            return _orderItems;
        }
        set => _orderItems = value;
    }

    /// <summary>
    ///     Gets or sets the order status
    /// </summary>
    [JsonProperty("order_status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public OrderStatus? OrderStatus { get; set; }

    /// <summary>
    ///     Gets or sets the payment status
    /// </summary>
    [JsonProperty("payment_status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public PaymentStatus? PaymentStatus { get; set; }

    /// <summary>
    ///     Gets or sets the shipping status
    /// </summary>
    [JsonProperty("shipping_status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ShippingStatus? ShippingStatus { get; set; }

    /// <summary>
    /// Gets or sets the paid date and time
    /// </summary>
    [JsonProperty("paid_date_utc")]
    public DateTime? PaidDateUtc { get; set; }


    /// <summary>
    ///    Gets or sets the date and time of instance update
    ///    </summary>
    [JsonProperty("paid_date_ts")]
    public long? PaidDateTs { get => PaidDateUtc == null ? null : DTOHelper.DateTimeToTimestamp((DateTime)PaidDateUtc); }
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
 
 */