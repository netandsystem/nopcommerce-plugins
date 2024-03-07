using Nop.Core.Domain.Statistics;
using Nop.Data;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTOs.Statistics;
using Nop.Plugin.Api.MappingExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Services;

#nullable enable
public class SellerStatisticsApiService : BaseSyncService<SellerStatisticsDto>, ISellerStatisticsApiService
{
    #region Fields

    private readonly IRepository<SellerStatistics> _sellerStatisticsRepository;

    #endregion

    #region Ctr

    public SellerStatisticsApiService(IRepository<SellerStatistics> sellerStatisticsRepository)
    {
        _sellerStatisticsRepository = sellerStatisticsRepository;
    }

    #endregion

    #region Methods

    public async Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int sellerId
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

        IList<int> _idsInDb = idsInDb ?? new List<int>();

        var selectedItemsQuery = from s in _sellerStatisticsRepository.Table
                                 where s.SellerId == sellerId
                                 select s.ToDto();

        var selectedItems = await selectedItemsQuery.ToListAsync();


        var selectedItemsIds = selectedItems.Select(x => x.Id).ToList();

        var itemsToInsertOrUpdate = selectedItems.Where(x =>
        {
            var d = _idsInDb.Contains(x.Id);
            var u = lastUpdateUtc == null || x.UpdatedOnUtc > lastUpdateUtc;

            return !d || u;
        }).ToList();

        var idsToDelete = _idsInDb.Where(x => !selectedItemsIds.Contains(x)).ToList();

        var itemsToSave = GetItemsCompressed(itemsToInsertOrUpdate);

        return new BaseSyncResponse(itemsToSave, idsToDelete);
    }

    public List<List<object?>> GetItemsCompressed(IList<SellerStatisticsDto> items)
    {
        /**
          [
             id, number
             deleted,  boolean
             updated_on_ts,  number
     
             seller_id, number
            month, number
            total_invoiced, number
            total_collected, number
            activations, number
          ]
          */

        return items.Select(p =>
            new List<object?>() {
                p.Id,
                p.Deleted,
                p.UpdatedOnTs,
                p.SellerId,
                p.Month,
                p.TotalInvoiced,
                p.TotalCollected,
                p.Activations
            }
        ).ToList();
    }


    public override async Task<BaseSyncResponse> GetLastestUpdatedItems3Async(
       IList<int>? idsInDb, long? lastUpdateTs, int sellerId, int storeId
    )
    {
        async Task<List<SellerStatisticsDto>> GetSellerItemsAsync()
        {
            var selectedItemsQuery = from s in _sellerStatisticsRepository.Table
                                     where s.SellerId == sellerId
                                     select s.ToDto();

            return await selectedItemsQuery.ToListAsync();
        }


        return await GetLastestUpdatedItems3Async(
            idsInDb,
            lastUpdateTs,
            () => GetSellerItemsAsync()
         );
    }

    public override List<List<object?>> GetItemsCompressed3(IList<SellerStatisticsDto> items)
    {
        /**
          [
            id, number
            deleted,  boolean
            updated_on_ts,  number
     
            seller_id, number
            month, number
            total_invoiced, number
            total_collected, number
            activations, number
          ]
          */

        return items.Select(p =>
            new List<object?>() {
                p.Id,
                p.Deleted,
                p.UpdatedOnTs,
                p.SellerId,
                p.Month,
                p.TotalInvoiced,
                p.TotalCollected,
                p.Activations
            }
        ).ToList();
    }

    #endregion

    #region Private Methods
    #endregion
}
