using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Models.Base;

#nullable enable

public class Sync2ParametersModel
{
    public Sync2ParametersModel(List<int> ids, long? lastUpdateTs, string? fields)
    {
        Ids = ids;
        LastUpdateTs = lastUpdateTs;
        Fields = fields;
    }

    [JsonProperty("ids", Required = Required.Always)]
    public List<int> Ids { get; set; }

    [JsonProperty("last_update_ts", Required = Required.AllowNull)]
    public long? LastUpdateTs { get; set; }

    [JsonProperty("fields", Required = Required.AllowNull)]
    public string? Fields { get; set; }
}
