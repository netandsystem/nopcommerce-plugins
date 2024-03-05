using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.Authorization.Attributes;
using Nop.Plugin.Api.Delta;
using Nop.Plugin.Api.DTO;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTO.Errors;
using Nop.Plugin.Api.Factories;
using Nop.Plugin.Api.Helpers;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.ActionResults;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Plugin.Api.ModelBinders;
using Nop.Plugin.Api.Models.CustomersParameters;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Core.Events;
using Nop.Core.Domain.Gdpr;
using Nop.Services.Gdpr;
using Nop.Core.Domain.Localization;
using Nop.Plugin.Api.DTOs.ShoppingCarts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Policies;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Models.Base;
using MySqlX.XDevAPI.Common;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/customers")]

public class CustomersController : BaseSyncController<CustomerDto>
{
    #region Fields

    private readonly ICountryService _countryService;
    private readonly ICustomerApiService _customerApiService;
    private readonly ICustomerRolesHelper _customerRolesHelper;
    private readonly IEncryptionService _encryptionService;
    private readonly IFactory<Customer> _factory;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly ILanguageService _languageService;
    private readonly IPermissionService _permissionService;
    private readonly IAddressService _addressService;
    private readonly ICurrencyService _currencyService;
    private readonly IMappingHelper _mappingHelper;
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private readonly ICustomerRegistrationService _customerRegistrationService;
    private readonly ICustomerAttributeService _customerAttributeService;
    private readonly ICustomerAttributeParser _customerAttributeParser;
    private readonly IWorkContext _workContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly GdprSettings _gdprSettings;
    private readonly IGdprService _gdprService;
    private readonly IWorkflowMessageService _workflowMessageService;
    private readonly LocalizationSettings _localizationSettings;

    // We resolve the customer settings this way because of the tests.
    // The auto mocking does not support concreate types as dependencies. It supports only interfaces.
    private CustomerSettings? _customerSettings;

    private CustomerSettings CustomerSettings
    {
        get
        {
            _customerSettings ??= EngineContext.Current.Resolve<CustomerSettings>();

            return _customerSettings;
        }
    }

    #endregion

    #region Ctr
    public CustomersController(
        ICustomerApiService customerApiService,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        ICustomerRolesHelper customerRolesHelper,
        IGenericAttributeService genericAttributeService,
        IEncryptionService encryptionService,
        IFactory<Customer> factory,
        ICountryService countryService,
        IMappingHelper mappingHelper,
        INewsLetterSubscriptionService newsLetterSubscriptionService,
        IPictureService pictureService, ILanguageService languageService,
        IPermissionService permissionService,
        IAddressService addressService,
        ICurrencyService currencyService,
        ICustomerRegistrationService customerRegistrationService,
        ICustomerAttributeService customerAttributeService,
        ICustomerAttributeParser customerAttributeParser,
        IWorkContext workContext,
        IEventPublisher eventPublisher,
        GdprSettings gdprSettings,
        IGdprService gdprService,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings,
        IAuthenticationService authenticationService,
        IStoreContext storeContext
    ) :
        base(customerApiService, jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService, customerActivityService,
             localizationService, pictureService, authenticationService, storeContext)
    {
        _customerApiService = customerApiService;
        _factory = factory;
        _countryService = countryService;
        _mappingHelper = mappingHelper;
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _languageService = languageService;
        _permissionService = permissionService;
        _addressService = addressService;
        _currencyService = currencyService;
        _encryptionService = encryptionService;
        _genericAttributeService = genericAttributeService;
        _customerRolesHelper = customerRolesHelper;
        _customerRegistrationService = customerRegistrationService;
        _customerAttributeService = customerAttributeService;
        _customerAttributeParser = customerAttributeParser;
        _workContext = workContext;
        _eventPublisher = eventPublisher;
        _gdprSettings = gdprSettings;
        _gdprService = gdprService;
        _workflowMessageService = workflowMessageService;
        _localizationSettings = localizationSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Retrieve current customer
    /// </summary>
    /// <param name="fields">Fields from the customer you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("syncdata", Name = "SyncCustomers")]
    [Authorize(Policy = SellerRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    //[GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> SyncData(long? lastUpdateTs, string? fields)
    {
        var sellerEntity = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (sellerEntity is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        DateTime? lastUpdateUtc = null;

        if (lastUpdateTs.HasValue)
        {
            lastUpdateUtc = DTOHelper.TimestampToDateTime(lastUpdateTs.Value);
        }

        var result = await _customerApiService.GetLastestUpdatedCustomersAsync(
                lastUpdateUtc, sellerEntity.Id
            );

        result = await _customerApiService.JoinCustomerDtosWithCustomerAttributesAsync(result);

        var customerRootObject = new CustomersRootObject
        {
            Customers = result
        };

        return OkResult(customerRootObject, fields);
    }


    /// <summary>
    ///     Retrieve current customer
    /// </summary>
    /// <param name="fields">Fields from the customer you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("syncdata2", Name = "SyncCustomers2")]
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

        var result = await _customerApiService.GetLastestUpdatedItems2Async(
                body.IdsInDb,
                lastUpdateUtc,
                sellerEntity.Id
            );

        return Ok(result);
    }


    /// <summary>
    ///     Retrieve current customer
    /// </summary>
    /// <param name="fields">Fields from the customer you want your json to contain</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet(Name = "GetCurrentCustomer")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    //[GetRequestsErrorInterceptorActionFilter]
    public async Task<IActionResult> GetCurrentCustomer([FromQuery] string? fields)
    {
        var customerEntity = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customerEntity is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        var customerList = new List<Customer> { customerEntity };
        var customerDtoList = await _customerApiService.JoinCustomersWithAddressesAsync(customerList);
        customerDtoList = await _customerApiService.JoinCustomerDtosWithCustomerAttributesAsync(customerDtoList);
        var result = customerDtoList.FirstOrDefault();

        if (result == null)
        {
            return Error(HttpStatusCode.NotFound, "customer", "not found");
        }

        // 'first_name', 'last_name', 'identity_card', 'phone'
        result.FirstName = result.Attributes?.GetValueOrDefault("first_name") ?? "";
        result.LastName = result.Attributes?.GetValueOrDefault("last_name") ?? "";
        result.IdentityCard = result.Attributes?.GetValueOrDefault("cedula") ?? "0";
        result.Phone = result.Attributes?.GetValueOrDefault("phone") ?? "0";

        return OkResult(result, fields);
    }

    /// <summary>
    ///     Create a new customer
    /// </summary>
    /// <param name="customerDelta">customer data</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Route("signup", Name = "SignUp")]
    [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), 422)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> SignUp(
       CustomerPost newCustomer)
    {
        ILocalizationService _localizationService = LocalizationService;
        CustomerSettings _customerSettings = CustomerSettings;

        var model = newCustomer;

        // Here we display the errors if the validation has failed at some point.
        if (!ModelState.IsValid)
        {
            return Error();
        }
        //Register(RegisterModel model, string returnUrl, bool captchaValid, IFormCollection form)

        //check whether registration is allowed
        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return Error(HttpStatusCode.ServiceUnavailable, "register", "The registration Service is Unavailable");

        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        //if (await _customerService.IsRegisteredAsync(customer))
        //{
        //    //Already registered customer. 
        //    await _authenticationService.SignOutAsync();

        //    //raise logged out event       
        //    await _eventPublisher.PublishAsync(new CustomerLoggedOutEvent(customer));

        //    //Save a new record
        //    await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
        //}



        var store = await _storeContext.GetCurrentStoreAsync();
        customer.RegisteredInStoreId = store.Id;

        var CustomCustomerAttributes = new
        {
            Cedula = model.IdentityCard
        };

        string jsonAttributes = JsonSerializer.Serialize(CustomCustomerAttributes);

        //custom customer attributes
        var customerAttributesXml = await ParseCustomCustomerAttributesAsync(jsonAttributes);
        var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
        foreach (var error in customerAttributeWarnings)
        {
            ModelState.AddModelError("", error);
        }

        ////validate CAPTCHA
        //if (_captchaSettings.Enabled && _captchaSettings.ShowOnRegistrationPage && !captchaValid)
        //{
        //    ModelState.AddModelError("", await _localizationService.GetResourceAsync("Common.WrongCaptchaMessage"));
        //}

        //GDPR
        //if (_gdprSettings.GdprEnabled)
        //{
        //    var consents = (await _gdprService
        //        .GetAllConsentsAsync()).Where(consent => consent.DisplayDuringRegistration && consent.IsRequired).ToList();

        //    ValidateRequiredConsents(consents, form);
        //}

        if (ModelState.IsValid)
        {
            customer.IsSystemAccount = false;

            var customerUserName = model.Username?.Trim();
            //var customerEmail = model.Email?.Trim();

            var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;

            var registrationRequest = new CustomerRegistrationRequest(
                customer,
                customerUserName,
                customerUserName,
                model.Password,
                _customerSettings.DefaultPasswordFormat,
                store.Id,
                isApproved);

            var registrationResult = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);

            if (registrationResult.Success)
            {
                ////properties
                //if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                //{
                //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.TimeZoneIdAttribute, model.TimeZoneId);
                //}
                ////VAT number
                //if (_taxSettings.EuVatEnabled)
                //{
                //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.VatNumberAttribute, model.VatNumber);

                //    var (vatNumberStatus, _, vatAddress) = await _taxService.GetVatNumberStatusAsync(model.VatNumber);
                //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.VatNumberStatusIdAttribute, (int)vatNumberStatus);
                //    //send VAT number admin notification
                //    if (!string.IsNullOrEmpty(model.VatNumber) && _taxSettings.EuVatEmailAdminWhenNewVatSubmitted)
                //        await _workflowMessageService.SendNewVatSubmittedStoreOwnerNotificationAsync(customer, model.VatNumber, vatAddress, _localizationSettings.DefaultAdminLanguageId);
                //}

                //form fields
                if (_customerSettings.FirstNameEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                if (_customerSettings.LastNameEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                if (_customerSettings.PhoneEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                if (_customerSettings.AcceptPrivacyPolicyEnabled)
                {
                    //privacy policy is required
                    //GDPR
                    if (_gdprSettings.GdprEnabled && _gdprSettings.LogPrivacyPolicyConsent)
                    {
                        await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.PrivacyPolicy"));
                    }
                }


                //save customer attributes
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);

                //notifications
                if (_customerSettings.NotifyNewCustomerRegistration)
                    await _workflowMessageService.SendCustomerRegisteredNotificationMessageAsync(customer,
                        _localizationSettings.DefaultAdminLanguageId);

                //raise event       
                await _eventPublisher.PublishAsync(new CustomerRegisteredEvent(customer));
                var currentLanguage = await _workContext.GetWorkingLanguageAsync();

                switch (_customerSettings.UserRegistrationType)
                {
                    case UserRegistrationType.EmailValidation:
                        //email validation message
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, Guid.NewGuid().ToString());
                        await _workflowMessageService.SendCustomerEmailValidationMessageAsync(customer, currentLanguage.Id);

                        //result
                        //return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation, returnUrl });
                        return OkResult(new { UserRegistrationType = "EmailValidation" });

                    case UserRegistrationType.AdminApproval:
                        //return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval, returnUrl });
                        return OkResult(new { UserRegistrationType = "AdminApproval" });

                    case UserRegistrationType.Standard:
                        //send customer welcome message
                        await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, currentLanguage.Id);

                        //raise event       
                        await _eventPublisher.PublishAsync(new CustomerActivatedEvent(customer));

                        //returnUrl = Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard, returnUrl });
                        //return await _customerRegistrationService.SignInCustomerAsync(customer, returnUrl, true);
                        return OkResult(new { UserRegistrationType = "Standard" });

                    case UserRegistrationType.Disabled:
                        //result
                        //return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled, returnUrl });
                        return OkResult(new { UserRegistrationType = "Disabled" });

                    default:
                        return Error(HttpStatusCode.InternalServerError, "customer", "the UserRegistrationType is not supported");
                }
            }

            //errors
            foreach (var error in registrationResult.Errors)
                ModelState.AddModelError("signUpErrors", error);

            return BadRequest(ModelState);
        }

        return BadRequest(ModelState);
    }

    /// <summary>
    ///     Update current customer
    /// </summary>
    /// <param name="customerDelta">customer data</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut(Name = "UpdateCustomer")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), 422)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> UpdateCustomer(
        [FromBody]
        //[ModelBinder(typeof(JsonModelBinder<CustomerDto>))]
        Delta<CustomerDto> customerDelta)
    {
        var currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (currentCustomer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(currentCustomer.Id))
        {
            return AccessDenied();
        }

        // Here we display the errors if the validation has failed at some point.
        if (!ModelState.IsValid)
        {
            return Error();
        }

        customerDelta.Merge(currentCustomer);

        await CustomerService.UpdateCustomerAsync(currentCustomer);


        //password
        //if (!string.IsNullOrWhiteSpace(customerDelta.Dto.Password))
        //{
        //    await AddPasswordAsync(customerDelta.Dto.Password, currentCustomer);
        //}

        // TODO: Localization

        // Preparing the result dto of the new customer
        // We do not prepare the shopping cart items because we have a separate endpoint for them.
        var updatedCustomer = currentCustomer.ToDto();

        // This is needed because the entity framework won't populate the navigation properties automatically
        // and the country name will be left empty because the mapping depends on the navigation property
        // so we do it by hand here.
        //await PopulateAddressCountryNamesAsync(updatedCustomer);

        // Set the fist and last name separately because they are not part of the customer entity, but are saved in the generic attributes.

        //activity log
        await CustomerActivityService.InsertActivityAsync("UpdateCustomer", await LocalizationService.GetResourceAsync("ActivityLog.UpdateCustomer"), currentCustomer);

        return OkResult(updatedCustomer);
    }

    /// <summary>
    ///     Create a new address for current customer
    /// </summary>
    /// <param name="newAddress">address data</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [Route("address", Name = "CreateAddress")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [GetRequestsErrorInterceptorActionFilter]
    [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> CreateAddress([FromBody] AddressDto newAddress)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id))
        {
            return AccessDenied();
        }

        var address = await InsertNewCustomerAddressIfDoesNotExist(customer, newAddress);

        await CustomerService.UpdateCustomerAsync(customer);

        return OkResult(address.ToDto());
    }

    /// <summary>
    ///     Create a new address for current customer
    /// </summary>
    /// <param name="newAddress">address data</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [Route("address", Name = "UpdateAddress")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [GetRequestsErrorInterceptorActionFilter]
    [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> UpdateAddress([FromBody] AddressDto newAddress)
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id))
        {
            return AccessDenied();
        }

        Address address = await _addressService.GetAddressByIdAsync(newAddress.Id);

        if (address is null)
        {
            return Error(HttpStatusCode.NotFound, "addressId", $"Address with id {newAddress.Id} not found");
        }

        IList<Address> customerAddresses = await CustomerService.GetAddressesByCustomerIdAsync(customer.Id);

        if (!customerAddresses.ToList().Exists(x => x.Id == newAddress.Id))
        {
            return Error(HttpStatusCode.BadRequest, "addressId", $"Address with id {newAddress.Id} does not belong to customer with id {customer.Id}");
        }

        address.FirstName = newAddress.FirstName ?? address.FirstName;
        address.LastName = newAddress.LastName ?? address.LastName;
        address.Email = newAddress.Email ?? address.Email;
        address.Company = newAddress.Company ?? address.Company;
        address.CountryId = newAddress.CountryId ?? address.CountryId;
        address.StateProvinceId = newAddress.StateProvinceId ?? address.StateProvinceId;
        address.City = newAddress.City ?? address.City;
        address.Address1 = newAddress.Address1 ?? address.Address1;
        address.Address2 = newAddress.Address2 ?? address.Address2;
        address.ZipPostalCode = newAddress.ZipPostalCode ?? address.ZipPostalCode;
        address.PhoneNumber = newAddress.PhoneNumber ?? address.PhoneNumber;
        address.CustomAttributes = newAddress.CustomAttributes ?? address.CustomAttributes;

        if (!await _addressService.IsAddressValidAsync(address))
        {
            return Error(HttpStatusCode.BadRequest, "address", "Address is not valid");
        }

        await _addressService.UpdateAddressAsync(address);

        return OkResult(newAddress);

    }

    /// <summary>
    ///     Create a new address for current customer
    /// </summary>
    /// <param name="addressId">address Id</param>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete]
    [Route("address/{addressId}", Name = "DeleteAddress")]
    [Authorize(Policy = RegisterRoleAuthorizationPolicy.Name)]
    [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorsRootObject), 422)]
    [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> DeleteAddress(
        [FromRoute] int addressId
    )
    {
        var customer = await _authenticationService.GetAuthenticatedCustomerAsync();

        if (customer is null)
        {
            return Error(HttpStatusCode.Unauthorized);
        }

        if (!await CheckPermissions(customer.Id))
        {
            return AccessDenied();
        }

        Address address = await _addressService.GetAddressByIdAsync(addressId);
        if (address is null)
        {
            return Error(HttpStatusCode.NotFound, "addressId", $"Address with id {addressId} not found");
        }

        IList<Address> customerAddresses = await CustomerService.GetAddressesByCustomerIdAsync(customer.Id);

        if (!customerAddresses.ToList().Exists(x => x.Id == addressId))
        {
            return Error(HttpStatusCode.BadRequest, "addressId", $"Address with id {addressId} does not belong to customer with id {customer.Id}");
        }

        await CustomerService.RemoveCustomerAddressAsync(customer, address);

        return Ok();
    }

    #endregion

    #region Private methods

    private async Task<string?> ParseCustomCustomerAttributesAsync(string jsonAttributes)
    {
        if (string.IsNullOrEmpty(jsonAttributes))
            throw new ArgumentNullException(nameof(jsonAttributes));

        Dictionary<string, object>? dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonAttributes);

        if (dictionary is null) return null;

        var attributesXml = "";
        var attributes = await _customerAttributeService.GetAllCustomerAttributesAsync();

        foreach (var attribute in attributes)
        {
            var attributeValue = dictionary.GetValueOrDefault(attribute.Name);

            if (attributeValue == null)
            {
                if (attribute.IsRequired)
                {
                    throw new ArgumentNullException("Attribute " + attribute.Name + " is required");
                }
            }
            else
            {
                attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                            attribute, attributeValue.ToString());
            }
        }

        return attributesXml;
    }

    private async Task<Address> InsertNewCustomerAddressIfDoesNotExist(Customer customer, AddressDto newAddress)
    {
        var newAddressEntity = newAddress.ToEntity();

        //try to find an address with the same values (don't duplicate records)
        var allCustomerAddresses = await CustomerService.GetAddressesByCustomerIdAsync(customer.Id);
        var address = _addressService.FindAddress(allCustomerAddresses.ToList(),
            newAddressEntity.FirstName, newAddressEntity.LastName, newAddressEntity.PhoneNumber,
            newAddressEntity.Email, newAddressEntity.FaxNumber, newAddressEntity.Company,
            newAddressEntity.Address1, newAddressEntity.Address2, newAddressEntity.City,
            newAddressEntity.County, newAddressEntity.StateProvinceId, newAddressEntity.ZipPostalCode,
            newAddressEntity.CountryId, newAddressEntity.CustomAttributes);

        if (address is null)
        {
            //address is not found. let's create a new one
            address = newAddressEntity;
            address.CreatedOnUtc = DateTime.UtcNow;

            //some validation
            if (address.CountryId == 0)
                address.CountryId = null;
            if (address.StateProvinceId == 0)
                address.StateProvinceId = null;

            await _addressService.InsertAddressAsync(address);

            await CustomerService.InsertCustomerAddressAsync(customer, address);
        }

        return address;
    }

    private async Task<bool> CheckPermissions(int customerId)
    {
        var currentCustomer = await _authenticationService.GetAuthenticatedCustomerAsync();
        if (currentCustomer is null)
            return false; // should not happen
        if (currentCustomer.Id == customerId)
        {
            return true;
        }
        // if I want to handle other customer's info, check admin permission
        return await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers, currentCustomer);
    }

    private async Task InsertFirstAndLastNameGenericAttributesAsync(string firstName, string lastName, Customer newCustomer)
    {
        // we assume that if the first name is not sent then it will be null and in this case we don't want to update it
        if (firstName != null)
        {
            await _genericAttributeService.SaveAttributeAsync(newCustomer, NopCustomerDefaults.FirstNameAttribute, firstName);
        }

        if (lastName != null)
        {
            await _genericAttributeService.SaveAttributeAsync(newCustomer, NopCustomerDefaults.LastNameAttribute, lastName);
        }
    }

    //private async Task PopulateAddressCountryNamesAsync(CustomerDto newCustomerDto)
    //{
    //    foreach (var address in newCustomerDto.Addresses)
    //    {
    //        await SetCountryNameAsync(address);
    //    }

    //    if (newCustomerDto.BillingAddress != null)
    //    {
    //        await SetCountryNameAsync(newCustomerDto.BillingAddress);
    //    }
    //}

    //private async Task SetCountryNameAsync(AddressDto address)
    //{
    //    if (string.IsNullOrEmpty(address.CountryName) && address.CountryId.HasValue)
    //    {
    //        var country = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
    //        address.CountryName = country.Name;
    //    }
    //}

    private async Task AddPasswordAsync(string newPassword, Customer customer)
    {
        // TODO: call this method before inserting the customer.
        var customerPassword = new CustomerPassword
        {
            CustomerId = customer.Id,
            PasswordFormat = CustomerSettings.DefaultPasswordFormat,
            CreatedOnUtc = DateTime.UtcNow
        };

        switch (CustomerSettings.DefaultPasswordFormat)
        {
            case PasswordFormat.Clear:
                {
                    customerPassword.Password = newPassword;
                }
                break;
            case PasswordFormat.Encrypted:
                {
                    customerPassword.Password = _encryptionService.EncryptText(newPassword);
                }
                break;
            case PasswordFormat.Hashed:
                {
                    var saltKey = _encryptionService.CreateSaltKey(5);
                    customerPassword.PasswordSalt = saltKey;
                    customerPassword.Password = _encryptionService.CreatePasswordHash(newPassword, saltKey, CustomerSettings.HashedPasswordFormat);
                }
                break;
        }

        await CustomerService.InsertCustomerPasswordAsync(customerPassword);

        await CustomerService.UpdateCustomerAsync(customer);
    }

    #endregion
}
