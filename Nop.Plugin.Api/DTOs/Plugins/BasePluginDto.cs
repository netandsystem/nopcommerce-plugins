using Newtonsoft.Json;
using Nop.Plugin.Api.DTO.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.DTOs.Plugins;

public class BasePluginDto : BaseDto
{
    /// <summary>
    ///     Gets or sets the system name
    /// </summary>
    [JsonProperty("system_name")]
    public string SystemName { get; set; }

    /// <summary>
    ///     Gets or sets the friendly name
    /// </summary>
    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; }
}
