using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.Images;
using Nop.Plugin.Api.DTO.Languages;
using Nop.Plugin.Api.DTO.SpecificationAttributes;

namespace Nop.Plugin.Api.DTO.Products;

[JsonObject(Title = "product")]
//[Validator(typeof(ProductDtoValidator))]

public class ProductDto : BaseSyncDto
{

    /// <summary>
    ///     Gets or sets the values indicating whether this product is visible in catalog or search results.
    ///     It's used when this product is associated to some "grouped" one
    ///     This way associated products could be accessed/added/etc only from a grouped product details page
    /// </summary>
    [JsonProperty("visible_individually")]
    public bool? VisibleIndividually { get; set; }

    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the short description
    /// </summary>
    [JsonProperty("short_description")]
    public string ShortDescription { get; set; }

    /// <summary>
    ///     Gets or sets the full description
    /// </summary>
    [JsonProperty("full_description")]
    public string FullDescription { get; set; }

    /// <summary>
    ///     Gets or sets the SKU
    /// </summary>
    [JsonProperty("sku")]
    public string Sku { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the product is marked as tax exempt
    /// </summary>
    [JsonProperty("is_tax_exempt")]
    public bool? IsTaxExempt { get; set; }

    /// <summary>
    ///     Gets or sets the price
    /// </summary>
    [JsonProperty("price")]
    public decimal? Price { get; set; }

#nullable enable

    [JsonProperty("images")]
    public List<string>? Images { get; set; }
#nullable disable

    [JsonProperty("stock_quantity")]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is published
    /// </summary>
    [JsonProperty("published")]
    public bool Published { get; set; }

    /// <summary>
    /// Product Category
    /// </summary>
    [JsonProperty("category_ids")]
    public List<int> CategoryIds { get; set; }
}
