using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.Images;
using Nop.Plugin.Api.DTO.Languages;
using Nop.Plugin.Api.DTO.SpecificationAttributes;
using Nop.Services.Payments;

namespace Nop.Plugin.Api.DTOs.ShippingMethod;

[JsonObject(Title = "shipping_method")]

public class ShippingMethodDto : BaseDto
{
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    [JsonProperty("name")]
    /// 
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    [JsonProperty("description")]
    /// 
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    [JsonProperty("display_order")]
    /// 
    public int DisplayOrder { get; set; }
}
