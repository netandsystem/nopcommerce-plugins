using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.DTO.ShoppingCarts;
using Nop.Plugin.Api.DTOs.ShippingMethod;

namespace Nop.Plugin.Api.MappingExtensions;

public static class ShippingMethodDtoMappings
{
    public static ShippingMethodDto ToDto(this ShippingMethod shippingMethod)
    {
        var shippingMethodDto = shippingMethod.MapTo<ShippingMethod, ShippingMethodDto>();

        return shippingMethodDto;
    }
}
