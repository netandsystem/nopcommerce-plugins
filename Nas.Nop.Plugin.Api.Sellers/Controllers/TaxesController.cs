using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTOs.Taxes;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.ModelBinders;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;

namespace Nop.Plugin.Api.Controllers;

public class TaxesController : BaseApiController
{
    private readonly ITaxCategoryService _taxCategoryService;
    private readonly IDTOHelper _dtoHelper;

    public TaxesController(
       IJsonFieldsSerializer jsonFieldsSerializer,
       IAclService aclService,
       ICustomerService customerService,
       IStoreMappingService storeMappingService,
       IStoreService storeService,
       IDiscountService discountService,
       ICustomerActivityService customerActivityService,
       ILocalizationService localizationService,
       IPictureService pictureService,
       ITaxCategoryService taxCategoryService, IDTOHelper dtoHelper) : base(jsonFieldsSerializer,
              aclService,
              customerService,
              storeMappingService,
              storeService,
              discountService,
              customerActivityService,
              localizationService,
              pictureService)
    {

        _taxCategoryService = taxCategoryService;
        _dtoHelper = dtoHelper;
    }

    [HttpGet]
    [Route("/api/taxcategories", Name = "getTaxCategories")]
    [ProducesResponseType(typeof(TaxCategoriesRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> getAllTaxCategories([FromQuery] string fields = "", [FromQuery] int? limit = 250)
    {
        var allTaxes = await _taxCategoryService.GetAllTaxCategoriesAsync();

        IList<TaxCategoryDto> taxCategoryDtos = new List<TaxCategoryDto>();

        foreach (var tax in allTaxes)
        {
            var taxDto = _dtoHelper.prepareTaxCategoryDto(tax);
            taxCategoryDtos.Add(taxDto);
        }

        var taxRoot = new TaxCategoriesRootObject
        {
            Taxes = taxCategoryDtos
        };

        var json = JsonFieldsSerializer.Serialize(taxRoot, fields);
        return new RawJsonActionResult(json);
    }

}
