using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.DTO.Categories;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Infrastructure;

namespace Nop.Plugin.Api.Services;

public interface ICategoryApiService : IBaseSyncService<CategoryDto>
{
    Category GetCategoryById(int categoryId);

    IList<Category> GetCategories(
        IList<int> ids = null,
        DateTime? createdAtMin = null, DateTime? createdAtMax = null, DateTime? updatedAtMin = null, DateTime? updatedAtMax = null,
        int limit = Constants.Configurations.DefaultLimit, int page = Constants.Configurations.DefaultPageValue,
        int sinceId = Constants.Configurations.DefaultSinceId,
        int? productId = null, bool? publishedStatus = null, int? parentCategoryId = null);

    Task<int> GetCategoriesCountAsync(
        DateTime? createdAtMin = null, DateTime? createdAtMax = null, DateTime? updatedAtMin = null, DateTime? updatedAtMax = null,
        bool? publishedStatus = null, int? productId = null, int? parentCategoryId = null);

    Task<IDictionary<int, IList<Category>>> GetProductCategories(IList<int> productIds);

    Task<List<CategoryDto>> GetLastestUpdatedCategoriesAsync(DateTime? lastUpdateUtc);

    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(DateTime? lastUpdateUtc);

#nullable enable
    List<List<object?>> GetItemsCompressed(IList<CategoryDto> items);
}
