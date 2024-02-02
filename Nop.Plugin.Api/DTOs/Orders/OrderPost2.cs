using Newtonsoft.Json;
using Nop.Plugin.Api.DTOs.ShoppingCarts;
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Api.DTO.Orders;

#nullable enable

[JsonObject(Title = "order")]
//[Validator(typeof(OrderDtoValidator))]
public class OrderPost2
{
    public OrderPost2(Guid orderGuid, int billingAddressId, List<ShoppingCartItemPost> orderItems, Dictionary<string, object> customValuesXml, int sellerId, Guid orderManagerGuid, decimal orderSubtotalInclTax, decimal orderSubtotalExclTax, decimal orderTax, decimal orderTotal, string paymentMethodSystemName)
    {
        OrderGuid = orderGuid;
        BillingAddressId = billingAddressId;
        OrderItems = orderItems;
        CustomValuesXml = customValuesXml;
        SellerId = sellerId;
        OrderManagerGuid = orderManagerGuid;
        OrderSubtotalInclTax = orderSubtotalInclTax;
        OrderSubtotalExclTax = orderSubtotalExclTax;
        OrderTax = orderTax;
        OrderTotal = orderTotal;
        PaymentMethodSystemName = paymentMethodSystemName;
    }



    /// <summary>
    /// Gets or sets the order identifier
    /// </summary>
    [JsonProperty("order_guid")]
    public Guid OrderGuid { get; set; }


    /// <summary>
    ///     Gets or sets the billing address
    /// </summary>
    [JsonProperty("billing_address_id")]
    public int BillingAddressId { get; set; }


    /// <summary>
    ///     Gets or sets the order's items
    /// </summary>
    [JsonProperty("order_items")]
    public List<ShoppingCartItemPost> OrderItems { get; set; } = new List<ShoppingCartItemPost>();

    /// <summary>
    ///     Gets or sets the billing address
    /// </summary>
    [JsonProperty("custom_values_xml")]
    public Dictionary<string, object> CustomValuesXml { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the associated seller
    /// </summary>
    [JsonProperty("seller_id")]
    public int SellerId { get; set; }

    /// <summary>
    /// Gets or sets the associated order manager guid
    /// </summary>
    [JsonProperty("order_manager_guid")]
    public Guid OrderManagerGuid { get; set; }

    /// <summary>
    ///     Gets or sets the order subtotal (incl tax)
    /// </summary>
    [JsonProperty("order_subtotal_incl_tax")]
    public decimal OrderSubtotalInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order subtotal (excl tax)
    /// </summary>
    [JsonProperty("order_subtotal_excl_tax")]
    public decimal OrderSubtotalExclTax { get; set; }

    /// <summary>
    ///     Gets or sets the order tax
    /// </summary>
    [JsonProperty("order_tax")]
    public decimal OrderTax { get; set; }

    /// <summary>
    ///     Gets or sets the order total
    /// </summary>
    [JsonProperty("order_total")]
    public decimal OrderTotal { get; set; }

    /// <summary>
    ///     Gets or sets the payment method system name
    /// </summary>
    [JsonProperty("payment_method_system_name")]
    public string PaymentMethodSystemName { get; set; }
}