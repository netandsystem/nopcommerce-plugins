using Newtonsoft.Json;
using Nop.Plugin.Api.DTO.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.DTOs.Statistics;

#nullable enable

public class SellerStatisticsDto : BaseSyncDto
{
    /// <summary>
    /// Gets or sets the seller identifier
    /// </summary>
    [JsonProperty("seller_id", Required = Required.Always)]
    public int SellerId { get; set; }

    /// <summary>
    /// Gets or sets the current month
    /// </summary>
    [JsonProperty("month", Required = Required.Always)]
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the total invoiced
    /// </summary>
    [JsonProperty("total_invoiced", Required = Required.Always)]
    public decimal TotalInvoiced { get; set; }

    /// <summary>
    /// Gets or sets the total collected
    /// </summary>
    [JsonProperty("total_collected", Required = Required.Always)]
    public decimal TotalCollected { get; set; }

    /// <summary>
    /// Gets or sets the Activations
    /// </summary>
    [JsonProperty("activations", Required = Required.Always)]
    public int Activations { get; set; }
}
