using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.DTOs.Base;

#nullable enable

//[JsonObject(Title = "base_sync_response")]
public class BaseSyncResponse
{
    public BaseSyncResponse(List<List<object?>> dataToSave, List<int>? dataToDelete = null)
    {
        DataToSave = dataToSave;
        CountToSave = dataToSave.Count;
        DataToDelete = dataToDelete ?? new List<int>();
        CountToDelete = dataToDelete?.Count ?? 0;
    }

    [JsonProperty("count_to_save")]
    public int CountToSave { get; set; }

    [JsonProperty("count_to_delete")]
    public int CountToDelete { get; set; }

    [JsonProperty("data_to_save")]
    public List<List<object?>> DataToSave { get; set; }

    [JsonProperty("data_to_delete")]
    public List<int> DataToDelete { get; set; }
}
