using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.DTO.ShoppingCarts;
using Nop.Plugin.Api.DTOs.ShoppingCarts;
using Nop.Plugin.Api.Infrastructure;

namespace Nop.Plugin.Api.Services;

public interface IShoppingCartItemApiService
{
    Task<List<ShoppingCartItem>> GetShoppingCartItemsAsync(
        int? customerId = null, DateTime? createdAtMin = null, DateTime? createdAtMax = null,
        DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, int? limit = null,
        int? page = null, ShoppingCartType? shoppingCartType = null);

    Task<List<ShoppingCartItemDto>> JoinShoppingCartItemsWithProductsAsync(IList<ShoppingCartItem> shoppingCartItems);

    Task<List<string>> AddProductListToCartAsync(
        Customer customer,
        List<ShoppingCartItemPost> newItems,
        int storeId
    );

    Task<List<string>> ReplaceCartAsync(
        Customer customer,
        List<ShoppingCartItemPost> newItems,
        int storeId
    );

    Task<List<string>> UpdateCartAsync(
        Customer customer,
        List<ShoppingCartItemPut> newItems,
        int storeId
    );

    Task<ShoppingCartItem> GetShoppingCartItemAsync(int id);

    Task EmptyCartAsync(int customerId, ShoppingCartType shoppingCartType);

    Task AddShoppingCartItemsToCartAsync(
       List<ShoppingCartItem> cart
    );
}
