using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Api.Infrastructure;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Configuration;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Plugin.Api.Configuration;
using Nop.Plugin.Api.Domain;
using Nop.Plugin.Api.Models.Authentication;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Nop.Services.Authentication;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Services.Discounts;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Core;
using System.Linq;
using Nop.Services.Configuration;
using Nop.Plugin.Api.MappingExtensions;

namespace Nop.Plugin.Api.Controllers;

[Route("api/token")]
[AllowAnonymous]
public class TokenController: BaseApiController
{
    #region Attributes
    private readonly ApiSettings _apiSettings;
    private readonly IAuthenticationService _authenticationService;
    private readonly ICustomerRegistrationService _customerRegistrationService;
    private readonly CustomerSettings _customerSettings;
    private readonly ILocalizationService _localizationService; 
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;


    #endregion

    #region Ctor
    public TokenController(
        ICustomerRegistrationService customerRegistrationService,
        ICustomerActivityService customerActivityService,
        IAuthenticationService authenticationService,
        CustomerSettings customerSettings,
        ApiSettings apiSettings,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        ISettingService settingService,
        IStoreContext storeContext
    ) :
    base(
        jsonFieldsSerializer,
        aclService,
        customerService,
        storeMappingService,
        storeService,
        discountService,
        customerActivityService,
        localizationService,
        pictureService
    )
    {
        _customerRegistrationService = customerRegistrationService;
        _authenticationService = authenticationService;
        _customerSettings = customerSettings;
        _apiSettings = apiSettings;
        _localizationService = localizationService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    #endregion

    #region Methods

    #nullable enable

    [HttpPost("login", Name = "TokenLogin")]
    [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> TokenLogin([FromBody] TokenRequest model)
    {
        Customer newCustomer;

        if (model.Guest)
        {
            newCustomer = await CustomerService.InsertGuestCustomerAsync();

            var tokenResponse = GenerateToken(newCustomer);

            await _authenticationService.SignInAsync(newCustomer, model.RememberMe); // update cookie-based authentication - not needed for api, avoids automatic generation of guest customer with each request to api

            // activity log
            await CustomerActivityService.InsertActivityAsync(newCustomer, "Api.TokenRequest", "API token request", newCustomer);

            return Ok(tokenResponse);
        }
        else
        {
            if (string.IsNullOrEmpty(model.Username))
            {
                return BadRequest("Missing username");
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Missing password");
            }

            var (LoginResult, error) = await LoginWithResultsAsync(model.Username, model.Password, model.RememberMe);

            if (error != null)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, new { loginError = error });
            }

            return Ok(LoginResult);
        }
    }

    [HttpPost("login/seller", Name = "SellerLogin")]
    [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> SellerLogin([FromBody] SellerLoginRequest model)
    {

        if (string.IsNullOrEmpty(model.Username))
        {
            return BadRequest("Missing username");
        }

        if (string.IsNullOrEmpty(model.Password))
        {
            return BadRequest("Missing password");
        }

        var (loginResult, loginError) = await LoginWithResultsAsync(model.Username, model.Password, true);

        if (loginError != null)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, new { loginError });
        }

        if (loginResult == null)
        {
            return BadRequest("Unknown Error - loginResult is null");
        }

        var seller = await CustomerService.GetCustomerByGuidAsync(loginResult.CustomerGuid);

        if (seller == null)
            return StatusCode((int)HttpStatusCode.Forbidden, new { loginError = "El vendedor no está autorizado" });

        var customerRoles = await CustomerService.GetCustomerRolesAsync(seller);
        var roleName = Constants.Roles.Seller.ToString();
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var apiSettings = await _settingService.LoadSettingAsync<ApiSettings>(storeScope);
        bool isRoleEnabled = apiSettings.ToModel().EnabledRolesDic[roleName];
        bool isCustomerInRole = customerRoles.FirstOrDefault(cr => cr.SystemName == roleName) != null;

        if (!isRoleEnabled)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, new { loginError = "El rol de cliente no está habilitado" });
        }

        if (!isCustomerInRole)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, new { loginError = "El vendedor no está autorizado" });
        }

        return Ok(loginResult);
    }

    #nullable disable

    [HttpGet("check", Name = "ValidateToken")]
    [Authorize(Policy = JwtBearerDefaults.AuthenticationScheme, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // this validates token
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> ValidateToken()
    {
        Customer currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync(); // this gets customer entity from db if it exists
        if (currentCustomer is null)
            return NotFound();
        return Ok();
    }

    #endregion

    #region Private methods

    #nullable enable
    private async Task<(TokenResponse? tokenResponse, string? loginError)> LoginWithResultsAsync(string username, string password, bool rememberMe)
    {
        var loginResult = await _customerRegistrationService.ValidateCustomerAsync(username, password);

        switch (loginResult)
        {
            case CustomerLoginResults.Successful:
                {
                    var customer = await (_customerSettings.UsernamesEnabled
                                       ? CustomerService.GetCustomerByUsernameAsync(username)
                                       : CustomerService.GetCustomerByEmailAsync(username));
                    //return customer;

                    var tokenResponse = GenerateToken(customer);

                    await _authenticationService.SignInAsync(customer, rememberMe); // update cookie-based authentication - not needed for api, avoids automatic generation of guest customer with each request to api

                    // activity log
                    await CustomerActivityService.InsertActivityAsync(customer, "Api.TokenRequest", "API token request", customer);

                    return (tokenResponse, null);
                }

            case CustomerLoginResults.CustomerNotExist:
                return (null, await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist"));
            case CustomerLoginResults.Deleted:
                return (null, await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.Deleted"));
            case CustomerLoginResults.NotActive:
                return (null, await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive"));
            case CustomerLoginResults.NotRegistered:
                return (null, await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotRegistered"));
            case CustomerLoginResults.LockedOut:
                return (null, await _localizationService.GetResourceAsync("Account.Login.WrongCredentials.LockedOut"));
            case CustomerLoginResults.WrongPassword:
            default:
                return (null, await _localizationService.GetResourceAsync("Account.Login.WrongCredentials"));
        }
    }

    #nullable disable

    private int GetTokenExpiryInDays()
    {
        return _apiSettings.TokenExpiryInDays <= 0
                   ? Constants.Configurations.DefaultAccessTokenExpirationInDays
                   : _apiSettings.TokenExpiryInDays;
    }

    private TokenResponse GenerateToken(Customer customer)
    {
        var currentTime = DateTimeOffset.Now;
        var expirationTime = currentTime.AddDays(GetTokenExpiryInDays());

        var claims = new List<Claim>
                         {
                             new Claim(JwtRegisteredClaimNames.Nbf, currentTime.ToUnixTimeSeconds().ToString()),
                             new Claim(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString()),
                             new Claim("CustomerId", customer.Id.ToString()),
                             new Claim(ClaimTypes.NameIdentifier, customer.CustomerGuid.ToString()),
                         };

        if (!string.IsNullOrEmpty(customer.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, customer.Email));
        }

        if (_customerSettings.UsernamesEnabled)
        {
            if (!string.IsNullOrEmpty(customer.Username))
            {
                claims.Add(new Claim(ClaimTypes.Name, customer.Username));
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(customer.Email))
            {
                claims.Add(new Claim(ClaimTypes.Name, customer.Email));
            }
        }
        var apiConfiguration = Singleton<AppSettings>.Instance.Get<ApiConfiguration>();
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiConfiguration.SecurityKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(new JwtHeader(signingCredentials), new JwtPayload(claims));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResponse(accessToken, currentTime.UtcDateTime, expirationTime.UtcDateTime)
        {
            CustomerId = customer.Id,
            CustomerGuid = customer.CustomerGuid,
            Username = _customerSettings.UsernamesEnabled ? customer.Username : customer.Email,
            TokenType = "Bearer",
        };
    }

    #endregion
}
