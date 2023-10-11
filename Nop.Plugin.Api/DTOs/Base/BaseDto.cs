using Newtonsoft.Json;

namespace Nop.Plugin.Api.DTO.Base;

#nullable enable

public abstract class BaseDto
{
    [JsonProperty("id")]
    public int Id { get; set; }
}
