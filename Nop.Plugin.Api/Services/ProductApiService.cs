using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nop.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Api.DataStructures;
using Nop.Plugin.Api.Infrastructure;
using Nop.Services.Stores;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Services.Localization;
using Nop.Core.Caching;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Shipping.Date;
using Nop.Core.Domain.Common;
using Nop.Plugin.Api.Models;
using Nop.Core.Domain.Media;
using SkiaSharp;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Nop.Services.Media;
using DocumentFormat.OpenXml.Math;
using LinqToDB.Common;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Nop.Plugin.Api.AutoMapper;
using System.Linq.Dynamic.Core;
using MySqlX.XDevAPI.Common;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.MappingExtensions;

namespace Nop.Plugin.Api.Services;

public class ProductApiService : IProductApiService
{
    private readonly IRepository<ProductCategory> _productCategoryMappingRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IStoreMappingService _storeMappingService;
    private readonly IRepository<Vendor> _vendorRepository;

    //------------------------------------------------

    protected readonly CatalogSettings _catalogSettings;
    protected readonly CommonSettings _commonSettings;
    protected readonly IAclService _aclService;
    protected readonly ICustomerService _customerService;
    protected readonly IDateRangeService _dateRangeService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IProductAttributeParser _productAttributeParser;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IRepository<CrossSellProduct> _crossSellProductRepository;
    protected readonly IRepository<DiscountProductMapping> _discountProductMappingRepository;
    protected readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    protected readonly IRepository<ProductAttributeCombination> _productAttributeCombinationRepository;
    protected readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
    protected readonly IRepository<ProductCategory> _productCategoryRepository;
    protected readonly IRepository<ProductManufacturer> _productManufacturerRepository;
    protected readonly IRepository<ProductPicture> _productPictureRepository;
    protected readonly IRepository<ProductProductTagMapping> _productTagMappingRepository;
    protected readonly IRepository<ProductReview> _productReviewRepository;
    protected readonly IRepository<ProductReviewHelpfulness> _productReviewHelpfulnessRepository;
    protected readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
    protected readonly IRepository<ProductTag> _productTagRepository;
    protected readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
    protected readonly IRepository<RelatedProduct> _relatedProductRepository;
    protected readonly IRepository<Shipment> _shipmentRepository;
    protected readonly IRepository<StockQuantityHistory> _stockQuantityHistoryRepository;
    protected readonly IRepository<TierPrice> _tierPriceRepository;
    protected readonly IRepository<Warehouse> _warehouseRepository;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IStoreService _storeService;
    protected readonly IWorkContext _workContext;
    protected readonly LocalizationSettings _localizationSettings;


    protected readonly IProductService _productService;
    private readonly IStoreContext _storeContext;
    private readonly INopFileProvider _fileProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MediaSettings _mediaSettings;
    private readonly IWebHelper _webHelper;
    private readonly IRepository<Picture> _pictureRepository;


    public ProductApiService(
        IRepository<Product> productRepository,
        IRepository<ProductCategory> productCategoryMappingRepository,
        IRepository<Vendor> vendorRepository,
        IStoreMappingService storeMappingService,

        //------------------------------------------------

        CatalogSettings catalogSettings,
        CommonSettings commonSettings,
        IAclService aclService,
        ICustomerService customerService,
        IDateRangeService dateRangeService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IRepository<CrossSellProduct> crossSellProductRepository,
        IRepository<DiscountProductMapping> discountProductMappingRepository,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<ProductPicture> productPictureRepository,
        IRepository<ProductProductTagMapping> productTagMappingRepository,
        IRepository<ProductReview> productReviewRepository,
        IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
        IRepository<ProductTag> productTagRepository,
        IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
        IRepository<RelatedProduct> relatedProductRepository,
        IRepository<Shipment> shipmentRepository,
        IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
        IRepository<TierPrice> tierPriceRepository,
        IRepository<Warehouse> warehouseRepository,
        IStaticCacheManager staticCacheManager,
        IStoreService storeService,
        IWorkContext workContext,
        LocalizationSettings localizationSettings,


        IProductService productService,
        IStoreContext storeContext,
        INopFileProvider fileProvider,
        IHttpContextAccessor httpContextAccessor,
        MediaSettings mediaSettings,
        IWebHelper webHelper,
        IRepository<Picture> pictureRepository

    )
    {
        _productRepository = productRepository;
        _productCategoryMappingRepository = productCategoryMappingRepository;
        _vendorRepository = vendorRepository;
        _storeMappingService = storeMappingService;

        //------------------------------------------------

        _catalogSettings = catalogSettings;
        _commonSettings = commonSettings;
        _aclService = aclService;
        _customerService = customerService;
        _dateRangeService = dateRangeService;
        _languageService = languageService;
        _localizationService = localizationService;
        _productAttributeParser = productAttributeParser;
        _productAttributeService = productAttributeService;
        _crossSellProductRepository = crossSellProductRepository;
        _discountProductMappingRepository = discountProductMappingRepository;
        _localizedPropertyRepository = localizedPropertyRepository;
        _productAttributeCombinationRepository = productAttributeCombinationRepository;
        _productAttributeMappingRepository = productAttributeMappingRepository;
        _productCategoryRepository = productCategoryRepository;
        _productManufacturerRepository = productManufacturerRepository;
        _productPictureRepository = productPictureRepository;
        _productTagMappingRepository = productTagMappingRepository;
        _productReviewRepository = productReviewRepository;
        _productReviewHelpfulnessRepository = productReviewHelpfulnessRepository;
        _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
        _productTagRepository = productTagRepository;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _relatedProductRepository = relatedProductRepository;
        _shipmentRepository = shipmentRepository;
        _stockQuantityHistoryRepository = stockQuantityHistoryRepository;
        _tierPriceRepository = tierPriceRepository;
        _warehouseRepository = warehouseRepository;
        _staticCacheManager = staticCacheManager;
        _storeService = storeService;
        _workContext = workContext;
        _localizationSettings = localizationSettings;

        _productService = productService;
        _storeContext = storeContext;
        _fileProvider = fileProvider;
        _httpContextAccessor = httpContextAccessor;
        _mediaSettings = mediaSettings;
        _webHelper = webHelper;
        _pictureRepository = pictureRepository;
    }

    public IList<Product> GetProducts(
        IList<int> ids = null,
        DateTime? createdAtMin = null, DateTime? createdAtMax = null, DateTime? updatedAtMin = null, DateTime? updatedAtMax = null,
        int? limit = null, int? page = null,
        int? sinceId = null,
        int? categoryId = null, string vendorName = null, bool? publishedStatus = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null)
    {
        var query = GetProductsQuery(createdAtMin, createdAtMax, updatedAtMin, updatedAtMax, vendorName, publishedStatus, ids, categoryId, manufacturerPartNumbers, isDownload);

        if (sinceId > 0)
        {
            query = query.Where(c => c.Id > sinceId);
        }

        return new ApiList<Product>(query, (page ?? Constants.Configurations.DefaultPageValue) - 1, (limit ?? Constants.Configurations.DefaultLimit));
    }

    public async Task<int> GetProductsCountAsync(
        DateTime? createdAtMin = null, DateTime? createdAtMax = null,
        DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, bool? publishedStatus = null, string vendorName = null,
        int? categoryId = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null)
    {
        var query = GetProductsQuery(createdAtMin, createdAtMax, updatedAtMin, updatedAtMax, vendorName,
                                     publishedStatus, ids: null, categoryId, manufacturerPartNumbers, isDownload);

        return await query.WhereAwait(async p => await _storeMappingService.AuthorizeAsync(p)).CountAsync();
    }

    public Product GetProductById(int productId)
    {
        if (productId == 0)
        {
            return null;
        }

        return _productRepository.Table.FirstOrDefault(product => product.Id == productId && !product.Deleted);
    }

    public Product GetProductByIdNoTracking(int productId)
    {
        if (productId == 0)
        {
            return null;
        }

        return _productRepository.Table.FirstOrDefault(product => product.Id == productId && !product.Deleted);
    }

    private IQueryable<Product> GetProductsQuery(
        DateTime? createdAtMin = null, DateTime? createdAtMax = null,
        DateTime? updatedAtMin = null, DateTime? updatedAtMax = null, string vendorName = null,
        bool? publishedStatus = null, IList<int> ids = null, int? categoryId = null, IList<string> manufacturerPartNumbers = null, bool? isDownload = null)

    {
        var query = _productRepository.Table;

        if (ids != null && ids.Count > 0)
        {
            query = query.Where(p => ids.Contains(p.Id));
        }

        if (manufacturerPartNumbers != null && manufacturerPartNumbers.Count > 0)
        {
            query = query.Where(p => manufacturerPartNumbers.Contains(p.ManufacturerPartNumber));
        }

        if (publishedStatus != null)
        {
            query = query.Where(p => p.Published == publishedStatus.Value);
        }

        if (isDownload != null)
        {
            query = query.Where(p => p.IsDownload == isDownload.Value);
        }

        // always return products that are not deleted!!!
        query = query.Where(p => !p.Deleted);

        if (createdAtMin != null)
        {
            query = query.Where(p => p.CreatedOnUtc > createdAtMin.Value);
        }

        if (createdAtMax != null)
        {
            query = query.Where(p => p.CreatedOnUtc < createdAtMax.Value);
        }

        if (updatedAtMin != null)
        {
            query = query.Where(p => p.UpdatedOnUtc > updatedAtMin.Value);
        }

        if (updatedAtMax != null)
        {
            query = query.Where(p => p.UpdatedOnUtc < updatedAtMax.Value);
        }

        if (!string.IsNullOrEmpty(vendorName))
        {
            query = from vendor in _vendorRepository.Table
                    join product in _productRepository.Table on vendor.Id equals product.VendorId
                    where vendor.Name == vendorName && !vendor.Deleted && vendor.Active
                    select product;
        }

        if (categoryId != null)
        {
            var categoryMappingsForProduct = from productCategoryMapping in _productCategoryMappingRepository.Table
                                             where productCategoryMapping.CategoryId == categoryId
                                             select productCategoryMapping;

            query = from product in query
                    join productCategoryMapping in categoryMappingsForProduct on product.Id equals productCategoryMapping.ProductId
                    select product;
        }

        query = query.OrderBy(product => product.Id);

        return query;
    }

    
    #nullable enable

    public virtual async Task<IPagedList<Product>> SearchProductsAsync(
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
    )
    {
        return await _productService.SearchProductsAsync(
                pageIndex: page - 1,
                pageSize: limit,
                categoryIds: !string.IsNullOrEmpty(categoryIds) ? categoryIds.Split(',').Select(int.Parse).ToList() : null,
                storeId: (await _storeContext.GetCurrentStoreAsync()).Id,
                visibleIndividuallyOnly: false,
                excludeFeaturedProducts: false,
                priceMin: priceMin,
                priceMax: priceMax,
                keywords: keywords,
                searchDescriptions: searchDescriptions ?? false,
                searchSku: searchSku ?? false,
                orderBy: orderBy ?? ProductSortingEnum.Position,
                showHidden: showHidden ?? false
            );
    }

    public async Task<List<ProductDto>> JoinProductsAndPicturesAsync(IList<Product> products)
    {
        var pictures = await GetProductsPicturesAsync(products);

        string imagePathUrl = await GetImagesPathUrlAsync();

        var query = from product in products
                    join picture in pictures
                    on product.Id equals picture.ProductId into productImagesGroup
                    select product.ToDto(productImagesGroup.Select(item => GetPictureUrl(item.Picture, imagePathUrl)).ToList());

        return query.ToList();
    }

    public async Task<ProductDto> AddPicturesToProductAsync(Product product)
    {
        var pictures = await GetProductsPicturesAsync(new List<Product>() { product });

        string imagePathUrl = await GetImagesPathUrlAsync();

        var productDto = product.ToDto(pictures.Select(item => GetPictureUrl(item.Picture, imagePathUrl)).ToList());

        return productDto;
    }

    #region Private methods

    private async Task<IList<InternalProductPicture>> GetProductsPicturesAsync(IList<Product> products)
    {
        var productPicturesQuery = GetProductPicturesQuery();

        var query = from pp in productPicturesQuery
                    join p in products
                    on pp.ProductId equals p.Id
                    select pp;

        return await query.ToListAsync();
    }

    private IQueryable<InternalProductPicture> GetProductPicturesQuery()
    {
        var query = from pp in _productPictureRepository.Table
                    join picture in _pictureRepository.Table
                    on pp.PictureId equals picture.Id
                    orderby pp.DisplayOrder, pp.Id
                    select new InternalProductPicture(picture, pp.ProductId);

        return query;
    }

    private string GetPictureUrl(Picture picture, string imagesPathUrl)
    {
        var seoFileName = picture.SeoFilename; // = GetPictureSeName(picture.SeoFilename); //just for sure

        var lastPart = GetFileExtensionFromMimeTypeAsync(picture.MimeType);

        string thumbFileName = !string.IsNullOrEmpty(seoFileName)
            ? $"{picture.Id:0000000}_{seoFileName}.{lastPart}"
            : $"{picture.Id:0000000}.{lastPart}";

        return GetThumbUrlAsync(thumbFileName, imagesPathUrl);
    }

    private string GetFileExtensionFromMimeTypeAsync(string mimeType)
    {
        var parts = mimeType.Split('/');
        var lastPart = parts[^1];
        switch (lastPart)
        {
            case "pjpeg":
                lastPart = "jpg";
                break;
            case "x-png":
                lastPart = "png";
                break;
            case "x-icon":
                lastPart = "ico";
                break;
            default:
                break;
        }

        return lastPart;
    }

    private string GetThumbUrlAsync(string thumbFileName, string imagesPathUrl)
    {
        var url = imagesPathUrl + "thumbs/";
        url += thumbFileName;
        return url;
    }

    private Task<string> GetImagesPathUrlAsync()
    {
        var pathBase = _httpContextAccessor?.HttpContext?.Request?.PathBase.Value ?? string.Empty;
        var imagesPathUrl = _mediaSettings.UseAbsoluteImagePath ? null : $"{pathBase}/";
        imagesPathUrl = string.IsNullOrEmpty(imagesPathUrl) ? _webHelper.GetStoreLocation() : imagesPathUrl;
        imagesPathUrl += "images/";

        return Task.FromResult(imagesPathUrl);
    }

    #endregion

    #region Private classes

    private class InternalProductPicture
    {
        public Picture Picture { get; set; }
        public int ProductId { get; set; }

        public InternalProductPicture(Picture picture, int productId)
        {
            Picture = picture;
            ProductId = productId;
        }
    }

    #endregion
}
