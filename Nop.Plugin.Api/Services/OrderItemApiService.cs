using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Api.DTO;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTO.OrderItems;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTOs.StateProvinces;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Services.Customers;
using Nop.Services.Directory;

namespace Nop.Plugin.Api.Services;

#nullable enable

public class OrderItemApiService : IOrderItemApiService
{
    #region Fields

    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IOrderApiService _orderApiService;

    #endregion

    #region Ctor

    public OrderItemApiService(
        IRepository<OrderItem> orderItemRepository,
        IOrderApiService orderApiService
    )
    {
        _orderItemRepository = orderItemRepository;
        _orderApiService = orderApiService;
    }

    #endregion

    #region Methods

    public async Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int sellerId
    )
    {
        var allOrders = await _orderApiService.GetLastedUpdatedOrders(null, sellerId);

        var query = from orderItem in _orderItemRepository.Table
                    join order in allOrders
                    on orderItem.OrderId equals order.Id
                    select orderItem.ToDto();

        /*  
            d = item in db
            s = item belongs to seller
            u = item updated after lastUpdateUtc

            s               // selected
            !d + u          // update o insert
            d!s             // delete
         
         */

        IList<int> _idsInDb = idsInDb ?? new List<int>();

        var selectedItems = await query.ToListAsync();
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

    #endregion

    #region Private Methods

    private List<List<object?>> GetItemsCompressed(IList<OrderItemDto> items)
    {
        /*
            [
              product_sku,  number
              unit_price_excl_tax,  number
              unit_price_incl_tax,  number
              quantity,  number
            ]
        */

        return items.Select(p =>
            new List<object?>() {
                p.ProductId,
                p.UnitPriceExclTax,
                p.UnitPriceInclTax,
                p.Quantity,
            }
        ).ToList();
    }


    #endregion

    #region Private Classes

    #endregion
}
