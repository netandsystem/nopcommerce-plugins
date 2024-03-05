using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Plugin.Api.DTO.Base;
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

public abstract class BaseSyncController<TDtoEntity> : BaseApiController where TDtoEntity : BaseSyncDto
{
    #region Fields
    protected readonly IAuthenticationService _authenticationService;
    protected readonly IBaseSyncService<TDtoEntity> _syncService;
    protected readonly IStoreContext _storeContext;
    #endregion

    #region Ctr
    public BaseSyncController(
        IBaseSyncService<TDtoEntity> syncService,
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
            IStoreContext storeContext)
        : base(jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService, customerActivityService, localizationService, pictureService)
    {
        _syncService = syncService;
        _authenticationService = authenticationService;
        _storeContext = storeContext;
    }
    #endregion

    /// <summary>
    ///     Retrieve current customer
    /// </summary>
    /// <param name="fields">Fields from the customer you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("syncdata2")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(BaseSyncResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> SyncData3(Sync2ParametersModel body)
    {
        var sellerEntity = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (sellerEntity is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var storeId = _storeContext.GetCurrentStore().Id;

        var result = await _syncService.GetLastestUpdatedItems3Async(
                body.IdsInDb,
                body.LastUpdateTs,
                sellerEntity.Id,
                storeId
            );

        return Ok(result);
    }


}