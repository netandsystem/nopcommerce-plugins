using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTOs.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Services;

#nullable enable

public interface ISellerStatisticsApiService : IBaseSyncService<SellerStatisticsDto>
{
    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int sellerId
    );

    List<List<object?>> GetItemsCompressed(IList<SellerStatisticsDto> items);
}
