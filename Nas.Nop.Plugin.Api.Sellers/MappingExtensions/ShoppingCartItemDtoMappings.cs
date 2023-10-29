using Nop.Core.Domain.Orders;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.DTO.ShoppingCarts;

namespace Nop.Plugin.Api.MappingExtensions;

public static class ShoppingCartItemDtoMappings
{
    public static ShoppingCartItemDto ToDto(this ShoppingCartItem shoppingCartItem, ProductDto productDto)
    {
        var shoppingCartItemDto = shoppingCartItem.MapTo<ShoppingCartItem, ShoppingCartItemDto>();

        shoppingCartItemDto.ProductDto = productDto;

        return shoppingCartItemDto;
    }
}
