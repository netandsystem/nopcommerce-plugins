using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Models.CustomersParameters;

#nullable enable

public class CustomersSync2ParametersModel
{
    public CustomersSync2ParametersModel(List<int> cutomersIds, long? lastUpdateTs, string? fields)
    {
        CutomersIds = cutomersIds;
        LastUpdateTs = lastUpdateTs;
        Fields = fields;
    }

    [JsonProperty("cutomers_ids", Required = Required.Always)]
    public List<int> CutomersIds { get; set; }

    [JsonProperty("last_update_ts", Required = Required.AllowNull)]
    public long? LastUpdateTs { get; set; }

    [JsonProperty("fields", Required = Required.AllowNull)]
    public string? Fields { get; set; }
}
