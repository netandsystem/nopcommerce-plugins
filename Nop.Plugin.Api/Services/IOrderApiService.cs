using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Api.DTO.Orders;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Infrastructure;
using Nop.Services.Orders;

namespace Nop.Plugin.Api.Services;

public interface IOrderApiService
{
    Task<List<OrderDto>> GetOrders(
        int? customerId,
        int? limit,
        int? page,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        ShippingStatus? shippingStatus,
        int? storeId,
        bool orderByDateDesc,
        DateTime? createdAtMin,
        DateTime? createdAtMax,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    );

    Task<PlaceOrderResult> PlaceOrderAsync(OrderPost newOrder, Customer customer, int storeId, IList<ShoppingCartItem> cart);

    Task<List<OrderDto>> GetLastestUpdatedItemsAsync(DateTime? lastUpdateUtc, int sellerId, int storeId);

#nullable enable

    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(DateTime? lastUpdateUtc, int sellerId, int storeId);

    List<List<object?>> GetItemsCompressed(IList<OrderDto> items);


    //IList<Order> GetOrders(
    //    IList<int> ids = null, DateTime? createdAtMin = null, DateTime? createdAtMax = null,
    //    int limit = Constants.Configurations.DefaultLimit, int page = Constants.Configurations.DefaultPageValue,
    //    int sinceId = Constants.Configurations.DefaultSinceId, OrderStatus? status = null, PaymentStatus? paymentStatus = null,
    //    ShippingStatus? shippingStatus = null, int? customerId = null, int? storeId = null, bool orderByDateDesc = false);

    //Order GetOrderById(int orderId);

    //int GetOrdersCount(
    //    DateTime? createdAtMin = null, DateTime? createdAtMax = null, OrderStatus? status = null,
    //    PaymentStatus? paymentStatus = null, ShippingStatus? shippingStatus = null,
    //    int? customerId = null, int? storeId = null);
}
