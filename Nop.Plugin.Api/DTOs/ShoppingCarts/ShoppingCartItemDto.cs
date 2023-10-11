using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTO.Products;
using Nop.Core.Domain.Orders;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Api.DTO.ShoppingCarts;

//[Validator(typeof(ShoppingCartItemDtoValidator))]
[JsonObject(Title = "shopping_cart_item")]
public class ShoppingCartItemDto : BaseDto
{
    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    [JsonProperty("quantity")]
    public int? Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of instance creation
    /// </summary>
    [JsonProperty("created_on_utc")]
    public DateTime? CreatedOnUtc { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of instance update
    /// </summary>
    [JsonProperty("updated_on_utc")]
    public DateTime? UpdatedOnUtc { get; set; }

    /// <summary>
    ///     Gets the log type
    /// </summary>
    [JsonProperty("shopping_cart_type", Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    public ShoppingCartType ShoppingCartType { get; set; }

    [JsonProperty("product_id")]
    public int? ProductId { get; set; }

    /// <summary>
    ///     Gets or sets the product
    /// </summary>
    [JsonProperty("product")]
    public ProductDto ProductDto { get; set; }
}
