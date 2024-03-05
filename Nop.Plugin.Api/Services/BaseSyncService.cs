using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Services;

#nullable enable

public abstract class BaseSyncService<TDtoEntity> : IBaseSyncService<TDtoEntity> where TDtoEntity : BaseSyncDto
{
    protected async Task<BaseSyncResponse> GetLastestUpdatedItems3Async(
        IList<int>? idsInDb,
        long? lastUpdateTs,
        Func<Task<List<TDtoEntity>>> GetSellerItemsAsync,
        Func<IList<TDtoEntity>, Task<List<TDtoEntity>>>? BeforeCompress = null
    )
    {
        /*  
            d = item in db
            s = item belongs to seller
            u = item updated after lastUpdateUtc

            s               // selected
            !d + u          // update o insert
            d!s             // delete
         
         */

        DateTime? lastUpdateUtc = null;

        if (lastUpdateTs is not null)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime((long)lastUpdateTs);
        }

        IList<int> _idsInDb = idsInDb ?? new List<int>();

        var selectedItems = await GetSellerItemsAsync();
        var selectedItemsIds = selectedItems.Select(x => x.Id).ToList();

        IList<TDtoEntity> itemsToInsertOrUpdate = selectedItems.Where(x =>
        {
            var d = _idsInDb.Contains(x.Id);
            var u = lastUpdateUtc == null || x.UpdatedOnUtc > lastUpdateUtc;

            return !d || u;
        }).ToList();

        var idsToDelete = _idsInDb.Where(x => !selectedItemsIds.Contains(x)).ToList();

        if (BeforeCompress != null)
        {
            itemsToInsertOrUpdate = await BeforeCompress(itemsToInsertOrUpdate);
        }

        var itemsToSave = GetItemsCompressed(itemsToInsertOrUpdate);

        return new BaseSyncResponse(itemsToSave, idsToDelete);
    }

    public abstract Task<BaseSyncResponse> GetLastestUpdatedItems3Async(
        IList<int>? idsInDb, long? lastUpdateTs, int sellerId, int storeId
    );

    public abstract List<List<object?>> GetItemsCompressed(IList<TDtoEntity> items);
}
