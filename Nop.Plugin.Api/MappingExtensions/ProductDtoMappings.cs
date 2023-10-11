using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTO.Products;
using System.Collections.Generic;

namespace Nop.Plugin.Api.MappingExtensions;

#nullable enable
public static class ProductDtoMappings
{
    public static ProductDto ToDto(this Product product, List<string>? images)
    {
        var productDto = product.MapTo<Product, ProductDto>();
        productDto.Images = images != null && images.Count == 0 ? null : images;
        return productDto;
    }

    public static ProductAttributeValueDto ToDto(this ProductAttributeValue productAttributeValue)
    {
        return productAttributeValue.MapTo<ProductAttributeValue, ProductAttributeValueDto>();
    }
}
