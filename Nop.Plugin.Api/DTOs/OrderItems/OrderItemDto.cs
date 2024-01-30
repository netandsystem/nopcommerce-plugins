using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.Products;

namespace Nop.Plugin.Api.DTO.OrderItems;

#nullable enable

//[Validator(typeof(OrderItemDtoValidator))]
[JsonObject(Title = "order_item")]
public class OrderItemDto : BaseSyncDto
{
    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the unit price in primary store currency (incl tax)
    /// </summary>
    [JsonProperty("unit_price_incl_tax")]
    public decimal UnitPriceInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the unit price in primary store currency (excl tax)
    /// </summary>
    [JsonProperty("unit_price_excl_tax")]
    public decimal UnitPriceExclTax { get; set; }

    /// <summary>
    ///     Gets or sets the price in primary store currency (incl tax)
    /// </summary>
    [JsonProperty("price_incl_tax")]
    public decimal PriceInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the price in primary store currency (excl tax)
    /// </summary>
    [JsonProperty("price_excl_tax")]
    public decimal PriceExclTax { get; set; }

    /// <summary>
    ///     Gets or sets the discount amount (incl tax)
    /// </summary>
    [JsonProperty("discount_amount_incl_tax")]
    public decimal DiscountAmountInclTax { get; set; }

    /// <summary>
    ///     Gets or sets the discount amount (excl tax)
    /// </summary>
    [JsonProperty("discount_amount_excl_tax")]
    public decimal DiscountAmountExclTax { get; set; }

    /// <summary>
    ///     Gets the product
    /// </summary>
    [JsonProperty("product")]
    [DoNotMap]
    public ProductDto? Product { get; set; }

    [JsonProperty("product_id")]
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the order identifier
    /// </summary>
    [JsonProperty("order_id")]
    public int OrderId { get; set; }
}
