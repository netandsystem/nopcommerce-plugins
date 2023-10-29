using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Security;
using Nop.Core;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Services.Customers;
using Nop.Services.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Stores;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO;

namespace Nop.Plugin.Api.Controllers;

[Authorize(Policy = JwtBearerDefaults.AuthenticationScheme, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
public class BaseApiController: ControllerBase
{
    protected readonly IAclService AclService;
    protected readonly ICustomerActivityService CustomerActivityService;
    protected readonly ICustomerService CustomerService;
    protected readonly IDiscountService DiscountService;
    protected readonly IJsonFieldsSerializer JsonFieldsSerializer;
    protected readonly ILocalizationService LocalizationService;
    protected readonly IPictureService PictureService;
    protected readonly IStoreMappingService StoreMappingService;
    protected readonly IStoreService StoreService;

    public BaseApiController(
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService)
    {
        JsonFieldsSerializer = jsonFieldsSerializer;
        AclService = aclService;
        CustomerService = customerService;
        StoreMappingService = storeMappingService;
        StoreService = storeService;
        DiscountService = discountService;
        CustomerActivityService = customerActivityService;
        LocalizationService = localizationService;
        PictureService = pictureService;
    }

    protected IActionResult Error(HttpStatusCode statusCode = (HttpStatusCode)422, string propertyKey = "", string errorMessage = "")
    {
        var errors = new Dictionary<string, List<string>>();

        if (!string.IsNullOrEmpty(errorMessage) && !string.IsNullOrEmpty(propertyKey))
        {
            var errorsList = new List<string>
                                 {
                                     errorMessage
                                 };
            errors.Add(propertyKey, errorsList);
        }

        foreach (var item in ModelState)
        {
            var errorMessages = item.Value.Errors.Select(x => x.ErrorMessage);

            var validErrorMessages = new List<string>();

            validErrorMessages.AddRange(errorMessages.Where(message => !string.IsNullOrEmpty(message)));

            if (validErrorMessages.Count > 0)
            {
                if (errors.ContainsKey(item.Key))
                {
                    errors[item.Key].AddRange(validErrorMessages);
                }
                else
                {
                    errors.Add(item.Key, validErrorMessages.ToList());
                }
            }
        }

        var errorsRootObject = new ErrorsRootObject
        {
            Errors = errors
        };

        var errorsJson = JsonSerializer.Serialize(errorsRootObject);

        return new ErrorActionResult(errorsJson, statusCode);
    }

    protected static IActionResult AccessDenied()
    {
        return new StatusCodeResult(Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden);
    }

    #nullable enable
    protected IActionResult OkResult(ISerializableObject itemDto, string? fields = null)
    {
        var json = JsonFieldsSerializer.Serialize(itemDto, fields ?? "");

        return new RawJsonActionResult(json);
    }

    protected IActionResult OkResult(object itemDto, string? fields = null)
    {
        var json = JsonFieldsSerializer.Serialize(itemDto, fields ?? "");

        return new RawJsonActionResult(json);
    }

    //protected bool IsApiRoleUser
}
