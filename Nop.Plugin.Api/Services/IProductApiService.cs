using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.Models;
using static Nop.Plugin.Api.Services.ProductApiService;
using static Nop.Services.ExportImport.ImportManager;

namespace Nop.Plugin.Api.Services;

public interface IProductApiService : IBaseSyncService<ProductDto>
{
    IList<Product> GetProducts(
        IList<int> ids = null,
        DateTime? createdAtMin = null, DateTime? createdAtMax = null, DateTime? updatedAtMin = null, DateTime? updatedAtMax = null,
        int? limit = null, int? page = null,
        int? sinceId = null,
        int? categoryId = null, string vendorName = null, bool? publishedStatus = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null);

    Task<int> GetProductsCountAsync(
        DateTime? createdAtMin = null, DateTime? createdAtMax = null,
        DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, bool? publishedStatus = null,
        string vendorName = null, int? categoryId = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null);

    Product GetProductById(int productId);

    Product GetProductByIdNoTracking(int productId);

#nullable enable

    Task<List<ProductDto>> JoinProductsAndPicturesAsync(IList<Product> products);

    Task<ProductDto> AddPicturesToProductAsync(Product product);

    Task<IPagedList<Product>> SearchProductsAsync(
        int page,
        int limit,
        string? categoryIds,
        decimal? priceMin,
        decimal? priceMax,
        string? keywords,
        bool? searchDescriptions,
        bool? searchSku,
        ProductSortingEnum? orderBy,
        bool? showHidden
    );

    Task<List<Product>> GetLastestUpdatedProducts(
        DateTime? lastUpdateUtc
    );

    Task<List<ProductDto>> JoinProductsAndCategoriesAsync(IList<ProductDto> products);

    Task<(List<SkuPicture> productsUpdated, List<SkuPicture> productsRejected)> ImportProductsPicturesFromJsonAsync(IList<SkuPicture> skuPictureList);

    List<List<object?>> GetItemsCompressed(IList<ProductDto> products);

    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        DateTime? lastUpdateUtc
    );

}
