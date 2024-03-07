using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using System.Threading.Tasks;
using Nop.Services.Catalog;
using Nop.Plugin.Api.Infrastructure;
using Nop.Core;
using System.Net;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Services.Security;
using Nop.Services.Customers;
using Nop.Services.Stores;
using Nop.Services.Discounts;
using Nop.Services.Logging;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Models.ProductsParameters;
using Nop.Plugin.Api.Services;
using Nop.Services.Seo;
using Nop.Services.Orders;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Policies;
using Azure;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using MailKit.Search;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static Nop.Services.ExportImport.ImportManager;
using Newtonsoft.Json;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Models.Base;
using Nop.Services.Authentication;

namespace Nop.Plugin.Api.Controllers;

[Route("api/products")]
public class ProductsController : BaseSyncController<ProductDto>
{
    #region Attributes

    private readonly IProductService _productService;
    private readonly IDTOHelper _dtoHelper;
    private readonly IProductApiService _productApiService;
    private readonly IOrderReportService _orderReportService;

    #endregion

    #region Ctr
    public ProductsController(
        IProductService productService,
        IStoreContext storeContext,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IProductApiService productApiService,
        IDTOHelper dtoHelper,
        IOrderReportService orderReportService,
        IAuthenticationService authenticationService
    ) :
    base(
        productApiService,
        jsonFieldsSerializer,
        aclService,
        customerService,
        storeMappingService,
        storeService,
        discountService,
        customerActivityService,
        localizationService,
        pictureService,
        authenticationService,
        storeContext
    )
    {
        _productService = productService;
        _productApiService = productApiService;
        _dtoHelper = dtoHelper;
        _orderReportService = orderReportService;
    }

    #endregion

    #region Methods

#nullable enable

    /// <summary>
    ///     Search products
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("search", Name = "Search")]
    [ProducesResponseType(typeof(ProductsRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> Search(
        int page,
        int limit,
        string? categoryIds,
        decimal? priceMin,
        decimal? priceMax,
        string? keywords,
        bool? searchDescriptions,
        bool? searchSku,
        ProductSortingEnum? orderBy,
        bool? showHidden,
        string? fields
    )
    {
        if (page < Constants.Configurations.DefaultPageValue)
        {
            return Error(HttpStatusCode.BadRequest, "page", "invalid page parameter");
        }

        if (limit < Constants.Configurations.MinLimit || limit > Constants.Configurations.MaxLimit)
        {
            return Error(HttpStatusCode.BadRequest, "limit", "invalid limit parameter");
        }

        var products = await _productApiService.SearchProductsAsync(
                page,
                limit,
                categoryIds,
                priceMin,
                priceMax,
                keywords,
                searchDescriptions,
                searchSku,
                orderBy,
                showHidden
            );

        var result = await _productApiService.JoinProductsAndPicturesAsync(products);

        var productsRootObject = new ProductsRootObjectDto
        {
            Products = result
        };

        return OkResult(productsRootObject, fields);
    }

    /// <summary>
    ///     Receive a list of all products
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetProducts")]
    [ProducesResponseType(typeof(ProductsRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetProducts([FromQuery] ProductsParametersModel parameters)
    {
        if (parameters.Limit < Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
        {
            return Error(HttpStatusCode.BadRequest, "limit", "invalid limit parameter");
        }

        if (parameters.Page < Constants.Configurations.DefaultPageValue)
        {
            return Error(HttpStatusCode.BadRequest, "page", "invalid page parameter");
        }

        var allProducts = _productApiService.GetProducts(parameters.Ids, parameters.CreatedAtMin, parameters.CreatedAtMax, parameters.UpdatedAtMin,
                                                         parameters.UpdatedAtMax, parameters.Limit, parameters.Page, parameters.SinceId, parameters.CategoryId,
                                                         parameters.VendorName, parameters.PublishedStatus, parameters.ManufacturerPartNumbers, parameters.IsDownload)
                                            .WhereAwait(async p => await StoreMappingService.AuthorizeAsync(p));

        var result = await _productApiService.JoinProductsAndPicturesAsync(await allProducts.ToListAsync());

        var productsRootObject = new ProductsRootObjectDto
        {
            Products = result
        };

        return OkResult(productsRootObject, parameters.Fields);
    }

    /// <summary>
    ///     Receive a count of all products
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("/api/products/count", Name = "GetProductsCount")]
    [ProducesResponseType(typeof(ProductsCountRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetProductsCount([FromQuery] ProductsCountParametersModel parameters)
    {
        var allProductsCount = await _productApiService.GetProductsCountAsync(parameters.CreatedAtMin, parameters.CreatedAtMax, parameters.UpdatedAtMin,
                                                                   parameters.UpdatedAtMax, parameters.PublishedStatus, parameters.VendorName,
                                                                   parameters.CategoryId, manufacturerPartNumbers: null, parameters.IsDownload);

        var productsCountRootObject = new ProductsCountRootObject
        {
            Count = allProductsCount
        };

        return Ok(productsCountRootObject);
    }

    /// <summary>
    ///     Retrieve product by spcified id
    /// </summary>
    /// <param name="id">Id of the product</param>
    /// <param name="fields">Fields from the product you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("/api/products/{id}", Name = "GetProductById")]
    [ProducesResponseType(typeof(ProductsRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetProductById([FromRoute] int id, [FromQuery] string fields = "")
    {
        if (id <= 0)
        {
            return Error(HttpStatusCode.BadRequest, "id", "invalid id");
        }

        var product = _productApiService.GetProductById(id);

        if (product == null)
        {
            return Error(HttpStatusCode.NotFound, "product", "not found");
        }

        var productDto = await _productApiService.AddPicturesToProductAsync(product);

        var productsRootObject = new ProductsRootObjectDto();

        productsRootObject.Products.Add(productDto);

        return OkResult(productsRootObject, fields);
    }

    /// <summary>
    ///     Get Best Sellers Products
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("bestsellers", Name = "GetBestsSellersProducts")]
    [ProducesResponseType(typeof(ProductsRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetBestsSellersProducts(
        int page,
        int limit,
        string? fields
    )
    {
        if (limit < Constants.Configurations.MinLimit || limit > Constants.Configurations.MaxLimit)
        {
            return Error(HttpStatusCode.BadRequest, "limit", "invalid limit parameter");
        }

        if (page < Constants.Configurations.DefaultPageValue)
        {
            return Error(HttpStatusCode.BadRequest, "page", "invalid page parameter");
        }

        //----------------------------------------------------------------------
        var report = await (await _orderReportService.BestSellersReportAsync(
             pageIndex: page - 1,
             pageSize: limit)).ToListAsync();

        //load products
        var products = await (await _productService.GetProductsByIdsAsync(report.Select(x => x.ProductId).ToArray()))
        //availability dates
        .Where(p => _productService.ProductIsAvailable(p)).ToListAsync();

        if (!products.Any())
        {
            return Error(HttpStatusCode.NotFound, "product", "not found");
        }
        //----------------------------------------------------------------------

        var result = await _productApiService.JoinProductsAndPicturesAsync(products);

        var productsRootObject = new ProductsRootObjectDto
        {
            Products = result
        };

        return OkResult(productsRootObject, fields);
    }

    [HttpGet]
    [Route("/api/products/categories", Name = "GetProductCategories")]
    [ProducesResponseType(typeof(ProductCategoriesRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetProductCategories([FromQuery] ProductCategoriesParametersModel parameters, [FromServices] ICategoryApiService categoryApiService)
    {
        if (parameters.ProductIds is null)
        {
            return Error(HttpStatusCode.BadRequest, "product_ids", "Product ids is null");
        }

        var productCategories = await categoryApiService.GetProductCategories(parameters.ProductIds);

        var productCategoriesRootObject = new ProductCategoriesRootObjectDto
        {
            ProductCategories = await productCategories.SelectAwait(async prodCats => new ProductCategoriesDto
            {
                ProductId = prodCats.Key,
                Categories = await prodCats.Value.SelectAwait(async cat => await _dtoHelper.PrepareCategoryDTOAsync(cat)).ToListAsync()
            }).ToListAsync()

            //ProductCategories = await productCategories.ToDictionaryAwaitAsync
            //(
            //	keySelector: prodCats => ValueTask.FromResult(prodCats.Key),
            //	elementSelector: async prodCats => await prodCats.Value.SelectAwait(async cat => await _dtoHelper.PrepareCategoryDTOAsync(cat)).ToListAsync()
            //)
        };

        return Ok(productCategoriesRootObject);
    }


    [HttpGet("syncdata", Name = "SyncProducts")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(ProductsRootObjectDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SyncData(long? lastUpdateTs, string? fields)
    {
        DateTime? lastUpdateUtc = null;

        if (lastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(lastUpdateTs.Value);
        }

        var products = await _productApiService.GetLastestUpdatedProducts(lastUpdateUtc);

        var result = await _productApiService.JoinProductsAndPicturesAsync(products);

        result = await _productApiService.JoinProductsAndCategoriesAsync(result);

        var productsRootObject = new ProductsRootObjectDto
        {
            Products = result
        };

        return OkResult(productsRootObject, fields);
    }


    [HttpPost("syncdata2", Name = "SyncProducts2")]
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

        var result = await _productApiService.GetLastestUpdatedItems2Async(lastUpdateUtc);

        return Ok(result);
    }

    [HttpPost("picture", Name = "ImportProductsPicturesFromJsonAsync")]
    [Authorize(Policy = AdministratorsRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(ImportPictureResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ImportProductsPicturesFromJsonAsync(List<SkuPicture> skuPictureList)
    {

        var (productsUpdated, productsRejected) = await _productApiService.ImportProductsPicturesFromJsonAsync(skuPictureList);

        ImportPictureResponse productsRootObject = new(productsUpdated, productsRejected);

        return OkResult(productsRootObject);
    }

    public class ImportPictureResponse
    {
        public ImportPictureResponse(List<SkuPicture> productsUpdated, List<SkuPicture> productsRejected)
        {
            ProductsUpdated = productsUpdated;
            CountUpdated = productsUpdated.Count;
            ProductsRejected = productsRejected;
            CountRejected = productsRejected.Count;
        }

        public int CountUpdated { get; set; }
        public int CountRejected { get; set; }
        public List<SkuPicture> ProductsUpdated { get; set; }
        public List<SkuPicture> ProductsRejected { get; set; }
    }
    #endregion
}
