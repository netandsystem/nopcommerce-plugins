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

public record ShoppingCartItemPut
{
    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    [JsonProperty("quantity" , Required = Required.Always)]
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets the product id
    /// </summary>
    [JsonProperty("id" , Required = Required.Always)]
    public int Id { get; set; }

}
