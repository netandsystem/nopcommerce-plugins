﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Media;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.DTO.Categories;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.Images;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.ModelBinders;
using Nop.Plugin.Api.Models.CategoriesParameters;
using Nop.Plugin.Api.Services;
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

public class CategoriesController : BaseApiController
{
    private readonly ICategoryApiService _categoryApiService;
    private readonly ICategoryService _categoryService;
    private readonly IDTOHelper _dtoHelper;
    private readonly IFactory<Category> _factory;
    private readonly IUrlRecordService _urlRecordService;

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
        IDTOHelper dtoHelper) : base(jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService,
                                     customerActivityService, localizationService, pictureService)
    {
        _categoryApiService = categoryApiService;
        _categoryService = categoryService;
        _urlRecordService = urlRecordService;
        _factory = factory;
        _dtoHelper = dtoHelper;
    }

    /// <summary>
    ///     Receive a list of all Categories
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("/api/categories", Name = "GetCategories")]
    [ProducesResponseType(typeof(CategoriesRootObject), (int) HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int) HttpStatusCode.BadRequest)]
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
    [HttpGet]
    [Route("/api/categories/count", Name = "GetCategoriesCount")]
    [ProducesResponseType(typeof(CategoriesCountRootObject), (int) HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int) HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int) HttpStatusCode.BadRequest)]
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
    [HttpGet]
    [Route("/api/categories/{id}", Name = "GetCategoryById")]
    [ProducesResponseType(typeof(CategoriesRootObject), (int) HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int) HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int) HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int) HttpStatusCode.Unauthorized)]
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
