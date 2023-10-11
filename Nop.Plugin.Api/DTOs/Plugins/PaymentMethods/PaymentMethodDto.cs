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

namespace Nop.Plugin.Api.DTOs.Plugins.PaymentMethods;

[JsonObject(Title = "payment_method")]

public class PaymentMethodDto : BasePluginDto
{
    /// <summary>
    ///     Gets or sets the short description
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    /// Gets the payment method images
    /// </summary>
    [JsonProperty("image")]
    public string Image { get; set; }
}
