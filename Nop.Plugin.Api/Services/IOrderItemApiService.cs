using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Api.DTO;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTOs.StateProvinces;

namespace Nop.Plugin.Api.Services;

#nullable enable

public interface IOrderItemApiService
{
    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int sellerId
    );

}
