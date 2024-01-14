using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.DTOs.Base;

#nullable enable

public class BaseSyncResponse<T>
{
    public BaseSyncResponse(List<T> dataToSave, List<int> dataToDelete)
    {
        DataToSave = dataToSave;
        CountToSave = dataToSave.Count;
        DataToDelete = dataToDelete;
        CountToDelete = dataToDelete.Count;
    }

    public int CountToSave { get; set; }

    public int CountToDelete { get; set; }

    public List<T> DataToSave { get; set; }

    public List<int> DataToDelete { get; set; }
}
