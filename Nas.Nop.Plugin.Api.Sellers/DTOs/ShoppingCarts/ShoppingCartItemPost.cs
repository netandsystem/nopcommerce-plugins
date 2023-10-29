using Newtonsoft.Json;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.DTO.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.DTOs.ShoppingCarts;

#nullable enable

public record ShoppingCartItemPost
{
    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    [JsonProperty("quantity" , Required = Required.Always)]
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets the shopping cart type
    /// </summary>
    [JsonProperty("shopping_cart_type", Required = Required.Always)]
    public ShoppingCartType ShoppingCartType { get; set; }

    /// <summary>
    ///     Gets the product id
    /// </summary>
    [JsonProperty("product_id" , Required = Required.Always)]
    public int ProductId { get; set; }

}
