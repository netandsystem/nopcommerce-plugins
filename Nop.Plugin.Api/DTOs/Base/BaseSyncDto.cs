using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Api.DTO.Base;

#nullable enable

public abstract class BaseSyncDto : BaseDto
{
    /// <summary>
    /// Gets or sets the date and time of product creation
    /// </summary>
    [JsonProperty("created_on_utc")]
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of product update
    /// </summary>
    [JsonProperty("updated_on_utc")]
    public DateTime UpdatedOnUtc { get; set; }

    // <summary>
    ///     Gets or sets the date and time of instance creation
    /// </summary>
    [JsonProperty("created_on_ts")]
    public long CreatedOnTs { set; get; }

    /// <summary>
    ///    Gets or sets the date and time of instance update
    ///    </summary>
    [JsonProperty("updated_on_ts")]
    public long UpdatedOnTs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity has been deleted
    /// </summary>
    [JsonProperty("deleted")]
    public bool Deleted { get; set; }
}
