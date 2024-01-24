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
    public Sync2ParametersModel(List<int> idsInDb, long? lastUpdateTs)
    {
        IdsInDb = idsInDb;
        LastUpdateTs = lastUpdateTs;
    }

    [JsonProperty("ids_in_db", Required = Required.AllowNull)]
    public List<int>? IdsInDb { get; set; }

    [JsonProperty("last_update_ts", Required = Required.AllowNull)]
    public long? LastUpdateTs { get; set; }
}
