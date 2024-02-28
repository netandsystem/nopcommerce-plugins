using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.Models.Base;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using System;
using System.Net;
using System.Threading.Tasks;


namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/seller-statistics")]

public class SellerStatisticsController : BaseApiController
{
    #region Fields

    private readonly IAuthenticationService _authenticationService;
    private readonly ISellerStatisticsApiService _sellerStatisticsApiService;


    #endregion

    #region Ctr

    public SellerStatisticsController(
        ICustomerApiService customerApiService,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IAuthenticationService authenticationService,
        ISellerStatisticsApiService sellerStatisticsApiService

    ) :
        base(jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService, customerActivityService,
             localizationService, pictureService)
    {
        _authenticationService = authenticationService;
        _sellerStatisticsApiService = sellerStatisticsApiService;
    }

    #endregion

    #region Methods
    /// <summary>
    ///     Retrieve current customer
    /// </summary>
    /// <param name="fields">Fields from the customer you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("syncdata2", Name = "SyncSellerStatistics2")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(BaseSyncResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    //[GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> SyncData2(Sync2ParametersModel body)
    {
        var sellerEntity = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (sellerEntity is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        DateTime? lastUpdateUtc = null;

        if (body.LastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(body.LastUpdateTs.Value);
        }

        var result = await _sellerStatisticsApiService.GetLastestUpdatedItems2Async(
                body.IdsInDb,
                lastUpdateUtc,
                sellerEntity.Id
            );

        return Ok(result);
    }

    #endregion

    #region Private methods


    #endregion
}
