using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTO.OrderItems;

namespace Nop.Plugin.Api.Services;

#nullable enable

public interface IOrderItemApiService : IBaseSyncService<OrderItemDto>
{
    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int sellerId
    );

}
