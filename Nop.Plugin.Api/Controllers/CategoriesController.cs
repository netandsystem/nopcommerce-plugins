using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Media;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.DTO.Categories;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.Images;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.ModelBinders;
using Nop.Plugin.Api.Models.Base;
using Nop.Plugin.Api.Models.CategoriesParameters;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/categories")]
public class CategoriesController : BaseSyncController<CategoryDto>
{
    #region Fields
    private readonly ICategoryApiService _categoryApiService;
    private readonly ICategoryService _categoryService;
    private readonly IDTOHelper _dtoHelper;
    private readonly IFactory<Category> _factory;
    private readonly IUrlRecordService _urlRecordService;
    #endregion

    #region Ctr

    public CategoriesController(
        ICategoryApiService categoryApiService,
        IJsonFieldsSerializer jsonFieldsSerializer,
        ICategoryService categoryService,
        IUrlRecordService urlRecordService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        IAclService aclService,
        ICustomerService customerService,
        IFactory<Category> factory,
        IDTOHelper dtoHelper,
        IAuthenticationService authenticationService,
        IStoreContext storeContext
        ) : base(categoryApiService, jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService,
                                     customerActivityService, localizationService, pictureService, authenticationService, storeContext)
    {
        _categoryApiService = categoryApiService;
        _categoryService = categoryService;
        _urlRecordService = urlRecordService;
        _factory = factory;
        _dtoHelper = dtoHelper;
    }

    #endregion

    [HttpGet("syncdata", Name = "SyncCategories")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(CategoriesRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SyncData(long? lastUpdateTs, string? fields)
    {
        DateTime? lastUpdateUtc = null;

        if (lastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(lastUpdateTs.Value);
        }

        var result = await _categoryApiService.GetLastestUpdatedCategoriesAsync(lastUpdateUtc);

        var rootObject = new CategoriesRootObject()
        {
            Categories = result
        };

        return OkResult(rootObject, fields);
    }

    [HttpPost("syncdata2", Name = "SyncCategories2")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(BaseSyncResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SyncData2(Sync2ParametersModel body)
    {
        DateTime? lastUpdateUtc = null;

        if (body.LastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(body.LastUpdateTs.Value);
        }

        var result = await _categoryApiService.GetLastestUpdatedItems2Async(lastUpdateUtc);

        return Ok(result);
    }

    /// <summary>
    ///     Receive a list of all Categories
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetCategories")]
    [ProducesResponseType(typeof(CategoriesRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetCategories([FromQuery] CategoriesParametersModel parameters)
    {
        if (parameters.Limit < Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
        {
            return Error(HttpStatusCode.BadRequest, "limit", "Invalid limit parameter");
        }

        if (parameters.Page < Constants.Configurations.DefaultPageValue)
        {
            return Error(HttpStatusCode.BadRequest, "page", "Invalid page parameter");
        }

        var allCategories = _categoryApiService.GetCategories(parameters.Ids, parameters.CreatedAtMin, parameters.CreatedAtMax,
                                                              parameters.UpdatedAtMin, parameters.UpdatedAtMax,
                                                              parameters.Limit, parameters.Page, parameters.SinceId,
                                                              parameters.ProductId, parameters.PublishedStatus, parameters.ParentCategoryId)
                                               .WhereAwait(async c => await StoreMappingService.AuthorizeAsync(c));

        IList<CategoryDto> categoriesAsDtos = await allCategories.SelectAwait(async category => await _dtoHelper.PrepareCategoryDTOAsync(category)).ToListAsync();

        var categoriesRootObject = new CategoriesRootObject
        {
            Categories = categoriesAsDtos
        };

        var json = JsonFieldsSerializer.Serialize(categoriesRootObject, parameters.Fields);

        return new RawJsonActionResult(json);
    }

    /// <summary>
    ///     Receive a count of all Categories
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("count", Name = "GetCategoriesCount")]
    [ProducesResponseType(typeof(CategoriesCountRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetCategoriesCount([FromQuery] CategoriesCountParametersModel parameters)
    {
        var allCategoriesCount = await _categoryApiService.GetCategoriesCountAsync(parameters.CreatedAtMin, parameters.CreatedAtMax,
                                                                        parameters.UpdatedAtMin, parameters.UpdatedAtMax,
                                                                        parameters.PublishedStatus, parameters.ProductId, parameters.ParentCategoryId);

        var categoriesCountRootObject = new CategoriesCountRootObject
        {
            Count = allCategoriesCount
        };

        return Ok(categoriesCountRootObject);
    }

    /// <summary>
    ///     Retrieve category by specified id
    /// </summary>
    /// <param name="id">Id of the category</param>
    /// <param name="fields">Fields from the category you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("{id}", Name = "GetCategoryById")]
    [ProducesResponseType(typeof(CategoriesRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetCategoryById([FromRoute] int id, [FromQuery] string fields = "")
    {
        if (id <= 0)
        {
            return Error(HttpStatusCode.BadRequest, "id", "invalid id");
        }

        var category = _categoryApiService.GetCategoryById(id);

        if (category == null)
        {
            return Error(HttpStatusCode.NotFound, "category", "category not found");
        }

        var categoryDto = await _dtoHelper.PrepareCategoryDTOAsync(category);

        var categoriesRootObject = new CategoriesRootObject();

        categoriesRootObject.Categories.Add(categoryDto);

        var json = JsonFieldsSerializer.Serialize(categoriesRootObject, fields);

        return new RawJsonActionResult(json);
    }
}
