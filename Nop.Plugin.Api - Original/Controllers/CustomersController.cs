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

namespace Nop.Plugin.Api.Controllers
{
    [AuthorizePermission("ManageCustomers")]
    public class CustomersController : BaseApiController
    {
        private readonly ICountryService _countryService;
        private readonly ICustomerApiService _customerApiService;
        private readonly ICustomerRolesHelper _customerRolesHelper;
        private readonly IEncryptionService _encryptionService;
        private readonly IFactory<Customer> _factory;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILanguageService _languageService;
        private readonly IPermissionService _permissionService;
        private readonly IAddressService _addressService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ICurrencyService _currencyService;
        private readonly IMappingHelper _mappingHelper;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly IWorkContext _workContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStoreContext _storeContext;
        private readonly GdprSettings _gdprSettings;
        private readonly IGdprService _gdprService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;

        // We resolve the customer settings this way because of the tests.
        // The auto mocking does not support concreate types as dependencies. It supports only interfaces.
        private CustomerSettings _customerSettings;

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
            IAuthenticationService authenticationService,
            ICurrencyService currencyService,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerAttributeService customerAttributeService,
            ICustomerAttributeParser customerAttributeParser,
            IWorkContext workContext,
            IEventPublisher eventPublisher,
            IStoreContext storeContext,
            GdprSettings gdprSettings,
            IGdprService gdprService,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings
        ) :
            base(jsonFieldsSerializer, aclService, customerService, storeMappingService, storeService, discountService, customerActivityService,
                 localizationService, pictureService)
        {
            _customerApiService = customerApiService;
            _factory = factory;
            _countryService = countryService;
            _mappingHelper = mappingHelper;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _languageService = languageService;
            _permissionService = permissionService;
            _addressService = addressService;
            _authenticationService = authenticationService;
            _currencyService = currencyService;
            _encryptionService = encryptionService;
            _genericAttributeService = genericAttributeService;
            _customerRolesHelper = customerRolesHelper;
            _customerRegistrationService = customerRegistrationService;
            _customerAttributeService = customerAttributeService;
            _customerAttributeParser = customerAttributeParser;
            _workContext = workContext;
            _eventPublisher = eventPublisher;
            _storeContext = storeContext;
            _gdprSettings = gdprSettings;
            _gdprService = gdprService;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = localizationSettings;
        }

        private CustomerSettings CustomerSettings
        {
            get
            {
                if (_customerSettings == null)
                {
                    _customerSettings = EngineContext.Current.Resolve<CustomerSettings>();
                }

                return _customerSettings;
            }
        }

        /// <summary>
        ///     Retrieve all customers of a shop
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/customers", Name = "GetCustomers")]
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [GetRequestsErrorInterceptorActionFilter]
        public async Task<IActionResult> GetCustomers([FromQuery] CustomersParametersModel parameters)
        {
            if (parameters.Limit < Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
            {
                return Error(HttpStatusCode.BadRequest, "limit", "Invalid limit parameter");
            }

            if (parameters.Page < Constants.Configurations.DefaultPageValue)
            {
                return Error(HttpStatusCode.BadRequest, "page", "Invalid request parameters");
            }

            var allCustomers = await _customerApiService.GetCustomersDtosAsync(parameters.CreatedAtMin, parameters.CreatedAtMax, parameters.Limit, parameters.Page, parameters.SinceId);

            var customersRootObject = new CustomersRootObject
            {
                Customers = allCustomers
            };

            var json = JsonFieldsSerializer.Serialize(customersRootObject, parameters.Fields);

            return new RawJsonActionResult(json);
        }

        /// <summary>
        ///     Retrieve customer by spcified id
        /// </summary>
        /// <param name="id">Id of the customer</param>
        /// <param name="fields">Fields from the customer you want your json to contain</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/customers/me", Name = "GetCurrentCustomer")]
        [AuthorizePermission("ManageCustomers", ignore: true)] // turn off all permission authorizations, access to this action is allowed to all authenticated customers
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [GetRequestsErrorInterceptorActionFilter]
        public async Task<IActionResult> GetCurrentCustomer([FromQuery] string fields)
        {
            var customerEntity = await _authenticationService.GetAuthenticatedCustomerAsync();

            if (customerEntity is null)
            {
                return Error(HttpStatusCode.Unauthorized);
            }

            var customerDto = await _customerApiService.GetCustomerByIdAsync(customerEntity.Id);

            if (customerDto == null)
            {
                return Error(HttpStatusCode.NotFound, "customer", "not found");
            }

            var customersRootObject = new CustomersRootObject();
            customersRootObject.Customers.Add(customerDto);

            var json = JsonFieldsSerializer.Serialize(customersRootObject, fields ?? "");

            return new RawJsonActionResult(json);
        }

        /// <summary>
        ///     Retrieve customer by spcified id
        /// </summary>
        /// <param name="id">Id of the customer</param>
        /// <param name="fields">Fields from the customer you want your json to contain</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/customers/{id}", Name = "GetCustomerById")]
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [GetRequestsErrorInterceptorActionFilter]
        public async Task<IActionResult> GetCustomerById([FromRoute] int id, [FromQuery] string fields = "")
        {
            if (id <= 0)
            {
                return Error(HttpStatusCode.BadRequest, "id", "invalid id");
            }

            var customer = await _customerApiService.GetCustomerByIdAsync(id);

            if (customer == null)
            {
                return Error(HttpStatusCode.NotFound, "customer", "not found");
            }

            var customersRootObject = new CustomersRootObject();
            customersRootObject.Customers.Add(customer);

            var json = JsonFieldsSerializer.Serialize(customersRootObject, fields);

            return new RawJsonActionResult(json);
        }


        /// <summary>
        ///     Get a count of all customers
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/customers/count", Name = "GetCustomersCount")]
        [ProducesResponseType(typeof(CustomersCountRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCustomersCount()
        {
            var allCustomersCount = await _customerApiService.GetCustomersCountAsync();

            var customersCountRootObject = new CustomersCountRootObject
            {
                Count = allCustomersCount
            };

            return Ok(customersCountRootObject);
        }

        /// <summary>
        ///     Search for customers matching supplied query
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("/api/customers/search", Name = "SearchCustomers")]
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Search([FromQuery] CustomersSearchParametersModel parameters)
        {
            if (parameters.Limit <= Constants.Configurations.MinLimit || parameters.Limit > Constants.Configurations.MaxLimit)
            {
                return Error(HttpStatusCode.BadRequest, "limit", "Invalid limit parameter");
            }

            if (parameters.Page <= 0)
            {
                return Error(HttpStatusCode.BadRequest, "page", "Invalid page parameter");
            }

            var customersDto = await _customerApiService.SearchAsync(parameters.Query, parameters.Order, parameters.Page, parameters.Limit);

            var customersRootObject = new CustomersRootObject
            {
                Customers = customersDto
            };

            var json = JsonFieldsSerializer.Serialize(customersRootObject, parameters.Fields);

            return new RawJsonActionResult(json);
        }

        [HttpPost]
        [Route("/api/customers", Name = "CreateCustomer")]
        [AuthorizePermission("ManageCustomers", ignore: true)]
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateCustomer(
            [FromBody]
            [ModelBinder(typeof(JsonModelBinder<CustomerDto>))]
            Delta<CustomerDto> customerDelta)
        {
            // Here we display the errors if the validation has failed at some point.
            if (!ModelState.IsValid)
            {
                return Error();
            }

            //If the validation has passed the customerDelta object won't be null for sure so we don't need to check for this.

            // Inserting the new customer
            var newCustomer = await _factory.InitializeAsync();
            customerDelta.Merge(newCustomer);

            foreach (var address in customerDelta.Dto.Addresses)
            {
                // we need to explicitly set the date as if it is not specified
                // it will default to 01/01/0001 which is not supported by SQL Server and throws and exception
                if (address.CreatedOnUtc == null)
                {
                    address.CreatedOnUtc = DateTime.UtcNow;
                }

                await CustomerService.InsertCustomerAddressAsync(newCustomer, address.ToEntity());
            }

            await CustomerService.InsertCustomerAsync(newCustomer);

            await InsertFirstAndLastNameGenericAttributesAsync(customerDelta.Dto.FirstName, customerDelta.Dto.LastName, newCustomer);

            if (customerDelta.Dto.LanguageId is int languageId && await _languageService.GetLanguageByIdAsync(languageId) != null)
            {
                await _genericAttributeService.SaveAttributeAsync(newCustomer, NopCustomerDefaults.LanguageIdAttribute, languageId);
            }

            if (customerDelta.Dto.CurrencyId is int currencyId && await _currencyService.GetCurrencyByIdAsync(currencyId) != null)
            {
                await _genericAttributeService.SaveAttributeAsync(newCustomer, NopCustomerDefaults.CurrencyIdAttribute, currencyId);
            }

            //phone
            if (!string.IsNullOrWhiteSpace(customerDelta.Dto.Phone))
                await _genericAttributeService.SaveAttributeAsync(newCustomer, NopCustomerDefaults.PhoneAttribute, customerDelta.Dto.Phone);

            //save customer attributes
            string customerAttributesXml;
            try
            {
                customerAttributesXml = await ParseCustomCustomerAttributesAsync(customerDelta.Dto.CustomCustomerAttributes);
            }
            catch (ArgumentException e)
            {
                return Error(HttpStatusCode.BadRequest, "CustomerAttributes", e.ParamName);
            }

            await _genericAttributeService.SaveAttributeAsync(newCustomer, NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);



            //password
            if (!string.IsNullOrWhiteSpace(customerDelta.Dto.Password))
            {
                await AddPasswordAsync(customerDelta.Dto.Password, newCustomer);
            }



            // We need to insert the entity first so we can have its id in order to map it to anything.
            // TODO: Localization
            // TODO: move this before inserting the customer.
            if (customerDelta.Dto.RoleIds.Count > 0)
            {
                await AddValidRolesAsync(customerDelta, newCustomer);
                await CustomerService.UpdateCustomerAsync(newCustomer);
            }


            // Preparing the result dto of the new customer
            // We do not prepare the shopping cart items because we have a separate endpoint for them.
            var newCustomerDto = newCustomer.ToDto();

            // This is needed because the entity framework won't populate the navigation properties automatically
            // and the country will be left null. So we do it by hand here.
            await PopulateAddressCountryNamesAsync(newCustomerDto);

            // Set the fist and last name separately because they are not part of the customer entity, but are saved in the generic attributes.
            newCustomerDto.FirstName = customerDelta.Dto.FirstName;
            newCustomerDto.LastName = customerDelta.Dto.LastName;

            newCustomerDto.Phone = customerDelta.Dto.Phone;
            newCustomerDto.CustomCustomerAttributes = customerAttributesXml;

            newCustomerDto.LanguageId = customerDelta.Dto.LanguageId;
            newCustomerDto.CurrencyId = customerDelta.Dto.CurrencyId;

            //activity log
            await CustomerActivityService.InsertActivityAsync("AddNewCustomer", await LocalizationService.GetResourceAsync("ActivityLog.AddNewCustomer"), newCustomer);

            var customersRootObject = new CustomersRootObject();

            customersRootObject.Customers.Add(newCustomerDto);

            var json = JsonFieldsSerializer.Serialize(customersRootObject, string.Empty);

            return new RawJsonActionResult(json);
        }

        [HttpPost]
        [Route("/api/customers/register", Name = "ApiRegister")]
        [AuthorizePermission("ManageCustomers", ignore: true)]
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ApiRegister(
            [FromBody]
            [ModelBinder(typeof(JsonModelBinder<CustomerDto>))]
            Delta<CustomerDto> customerDelta)
        {
            ICustomerService _customerService = CustomerService;
            ILocalizationService _localizationService = LocalizationService;
            CustomerSettings _customerSettings = CustomerSettings;

            var model = customerDelta.Dto;

            // Here we display the errors if the validation has failed at some point.
            if (!ModelState.IsValid)
            {
                return Error();
            }
            //Register(RegisterModel model, string returnUrl, bool captchaValid, IFormCollection form)

            //check whether registration is allowed
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                return Error(HttpStatusCode.ServiceUnavailable, "register", "The registration Service is Unavailable");

            var customer = await _workContext.GetCurrentCustomerAsync();

            if (await _customerService.IsRegisteredAsync(customer))
            {
                //Already registered customer. 
                await _authenticationService.SignOutAsync();

                //raise logged out event       
                await _eventPublisher.PublishAsync(new CustomerLoggedOutEvent(customer));

                //Save a new record
                await _workContext.SetCurrentCustomerAsync(await _customerService.InsertGuestCustomerAsync());
            }



            var store = await _storeContext.GetCurrentStoreAsync();
            customer.RegisteredInStoreId = store.Id;

            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributesAsync(model.CustomCustomerAttributes);
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
                var customerEmail = model.Email?.Trim();

                var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
                var registrationRequest = new CustomerRegistrationRequest(
                    customer,
                    customerEmail,
                    _customerSettings.UsernamesEnabled ? customerUserName : customerEmail,
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
                    if (_customerSettings.GenderEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.GenderAttribute, model.Gender);
                    if (_customerSettings.FirstNameEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                    if (_customerSettings.LastNameEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                    //if (_customerSettings.DateOfBirthEnabled)
                    //{
                    //    var dateOfBirth = model.ParseDateOfBirth();
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.DateOfBirthAttribute, dateOfBirth);
                    //}
                    if (_customerSettings.CompanyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CompanyAttribute, model.Company);
                    //if (_customerSettings.StreetAddressEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddressAttribute, model.StreetAddress);
                    //if (_customerSettings.StreetAddress2Enabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddress2Attribute, model.StreetAddress2);
                    //if (_customerSettings.ZipPostalCodeEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.ZipPostalCodeAttribute, model.ZipPostalCode);
                    //if (_customerSettings.CityEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CityAttribute, model.City);
                    //if (_customerSettings.CountyEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountyAttribute, model.County);
                    //if (_customerSettings.CountryEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountryIdAttribute, model.CountryId);
                    //if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StateProvinceIdAttribute,
                    //        model.StateProvinceId);
                    if (_customerSettings.PhoneEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                    //if (_customerSettings.FaxEnabled)
                    //    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FaxAttribute, model.Fax);

                    //newsletter
                    //if (_customerSettings.NewsletterEnabled)
                    //{
                    //var isNewsletterActive = _customerSettings.UserRegistrationType != UserRegistrationType.EmailValidation;

                    //save newsletter value
                    //var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customerEmail, store.Id);
                    //if (newsletter != null)
                    //{
                    //    if (model.Newsletter)
                    //    {
                    //        newsletter.Active = isNewsletterActive;
                    //        await _newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(newsletter);

                    //        //GDPR
                    //        if (_gdprSettings.GdprEnabled && _gdprSettings.LogNewsletterConsent)
                    //        {
                    //            await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.Newsletter"));
                    //        }
                    //    }
                    //    else
                    //    {
                    //        When registering, not checking the newsletter check box should not take an existing email address off of the subscription list.
                    //        _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletter);
                    //    }
                    //}
                    //else
                    //{
                    //    if (model.Newsletter)
                    //    {
                    //        await _newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
                    //        {
                    //            NewsLetterSubscriptionGuid = Guid.NewGuid(),
                    //            Email = customerEmail,
                    //            Active = isNewsletterActive,
                    //            StoreId = store.Id,
                    //            CreatedOnUtc = DateTime.UtcNow
                    //        });

                    //        //GDPR
                    //        if (_gdprSettings.GdprEnabled && _gdprSettings.LogNewsletterConsent)
                    //        {
                    //            await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.Newsletter"));
                    //        }
                    //    }
                    //}
                    //}

                    if (_customerSettings.AcceptPrivacyPolicyEnabled)
                    {
                        //privacy policy is required
                        //GDPR
                        if (_gdprSettings.GdprEnabled && _gdprSettings.LogPrivacyPolicyConsent)
                        {
                            await _gdprService.InsertLogAsync(customer, 0, GdprRequestType.ConsentAgree, await _localizationService.GetResourceAsync("Gdpr.Consent.PrivacyPolicy"));
                        }
                    }

                    ////GDPR
                    //if (_gdprSettings.GdprEnabled)
                    //{
                    //    var consents = (await _gdprService.GetAllConsentsAsync()).Where(consent => consent.DisplayDuringRegistration).ToList();
                    //    foreach (var consent in consents)
                    //    {
                    //        var controlId = $"consent{consent.Id}";
                    //        var cbConsent = form[controlId];
                    //        if (!StringValues.IsNullOrEmpty(cbConsent) && cbConsent.ToString().Equals("on"))
                    //        {
                    //            //agree
                    //            await _gdprService.InsertLogAsync(customer, consent.Id, GdprRequestType.ConsentAgree, consent.Message);
                    //        }
                    //        else
                    //        {
                    //            //disagree
                    //            await _gdprService.InsertLogAsync(customer, consent.Id, GdprRequestType.ConsentDisagree, consent.Message);
                    //        }
                    //    }
                    //}

                    //save customer attributes
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);

                    //insert default address (if possible)
                    var defaultAddress = new Address
                    {
                        FirstName = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.FirstNameAttribute),
                        LastName = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.LastNameAttribute),
                        Email = customer.Email,
                        Company = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CompanyAttribute),
                        CountryId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.CountryIdAttribute) > 0
                            ? (int?)await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.CountryIdAttribute)
                            : null,
                        StateProvinceId = await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.StateProvinceIdAttribute) > 0
                            ? (int?)await _genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.StateProvinceIdAttribute)
                            : null,
                        County = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CountyAttribute),
                        City = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.CityAttribute),
                        Address1 = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.StreetAddressAttribute),
                        Address2 = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.StreetAddress2Attribute),
                        ZipPostalCode = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.ZipPostalCodeAttribute),
                        PhoneNumber = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.PhoneAttribute),
                        FaxNumber = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.FaxAttribute),
                        CreatedOnUtc = customer.CreatedOnUtc
                    };
                    if (await _addressService.IsAddressValidAsync(defaultAddress))
                    {
                        //some validation
                        if (defaultAddress.CountryId == 0)
                            defaultAddress.CountryId = null;
                        if (defaultAddress.StateProvinceId == 0)
                            defaultAddress.StateProvinceId = null;
                        //set default address
                        //customer.Addresses.Add(defaultAddress);

                        await _addressService.InsertAddressAsync(defaultAddress);

                        await _customerService.InsertCustomerAddressAsync(customer, defaultAddress);

                        customer.BillingAddressId = defaultAddress.Id;
                        customer.ShippingAddressId = defaultAddress.Id;

                        await _customerService.UpdateCustomerAsync(customer);
                    }

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
                            return Ok(new { UserRegistrationType = "EmailValidation" });

                        case UserRegistrationType.AdminApproval:
                            //return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval, returnUrl });
                            return Ok(new { UserRegistrationType = "AdminApproval" });

                        case UserRegistrationType.Standard:
                            //send customer welcome message
                            await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, currentLanguage.Id);

                            //raise event       
                            await _eventPublisher.PublishAsync(new CustomerActivatedEvent(customer));

                            //returnUrl = Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard, returnUrl });
                            //return await _customerRegistrationService.SignInCustomerAsync(customer, returnUrl, true);
                            return Ok(new { UserRegistrationType = "Standard" });

                        case UserRegistrationType.Disabled:
                            //result
                            //return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled, returnUrl });
                            return Ok(new { UserRegistrationType = "Disabled" });

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

            //If we got this far, something failed, redisplay form
            //model = await _customerModelFactory.PrepareRegisterModelAsync(model, true, customerAttributesXml);


            //activity log

            //await CustomerActivityService.InsertActivityAsync("AddNewCustomer", await LocalizationService.GetResourceAsync("ActivityLog.AddNewCustomer"), customerDelta.T);

            //var customersRootObject = new CustomersRootObject();

            //customersRootObject.Customers.Add(model);

            //var json = JsonFieldsSerializer.Serialize(customersRootObject, string.Empty);

            //return new RawJsonActionResult(json);
        }

        protected virtual async Task<string> ParseCustomCustomerAttributesAsync(string jsonAttributes)
        {
            if (string.IsNullOrEmpty(jsonAttributes))
                throw new ArgumentNullException(nameof(jsonAttributes));

            Dictionary<string, object> dictionary = null;
            dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonAttributes);

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

        [HttpPut]
        [Route("/api/customers/{id}", Name = "UpdateCustomer")]
        [ProducesResponseType(typeof(CustomersRootObject), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateCustomer(
            [FromBody]
            [ModelBinder(typeof(JsonModelBinder<CustomerDto>))]
            Delta<CustomerDto> customerDelta)
        {
            // Here we display the errors if the validation has failed at some point.
            if (!ModelState.IsValid)
            {
                return Error();
            }

            // Updateting the customer
            var currentCustomer = await _customerApiService.GetCustomerEntityByIdAsync(customerDelta.Dto.Id);

            if (currentCustomer == null)
            {
                return Error(HttpStatusCode.NotFound, "customer", "not found");
            }

            customerDelta.Merge(currentCustomer);

            if (customerDelta.Dto.RoleIds.Count > 0)
            {
                await AddValidRolesAsync(customerDelta, currentCustomer);
            }

            if (customerDelta.Dto.Addresses.Count > 0)
            {
                var currentCustomerAddresses = (await CustomerService.GetAddressesByCustomerIdAsync(currentCustomer.Id)).ToDictionary(address => address.Id, address => address);

                foreach (var passedAddress in customerDelta.Dto.Addresses)
                {
                    var addressEntity = passedAddress.ToEntity();

                    if (currentCustomerAddresses.ContainsKey(passedAddress.Id))
                    {
                        _mappingHelper.Merge(passedAddress, currentCustomerAddresses[passedAddress.Id]);
                    }
                    else
                    {
                        await CustomerService.InsertCustomerAddressAsync(currentCustomer, addressEntity);
                    }
                }
            }

            await CustomerService.UpdateCustomerAsync(currentCustomer);

            await InsertFirstAndLastNameGenericAttributesAsync(customerDelta.Dto.FirstName, customerDelta.Dto.LastName, currentCustomer);


            if (customerDelta.Dto.LanguageId is int languageId && await _languageService.GetLanguageByIdAsync(languageId) != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.LanguageIdAttribute, languageId);
            }

            if (customerDelta.Dto.CurrencyId is int currencyId && await _currencyService.GetCurrencyByIdAsync(currencyId) != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.CurrencyIdAttribute, currencyId);
            }

            //password
            if (!string.IsNullOrWhiteSpace(customerDelta.Dto.Password))
            {
                await AddPasswordAsync(customerDelta.Dto.Password, currentCustomer);
            }

            // TODO: Localization

            // Preparing the result dto of the new customer
            // We do not prepare the shopping cart items because we have a separate endpoint for them.
            var updatedCustomer = currentCustomer.ToDto();

            // This is needed because the entity framework won't populate the navigation properties automatically
            // and the country name will be left empty because the mapping depends on the navigation property
            // so we do it by hand here.
            await PopulateAddressCountryNamesAsync(updatedCustomer);

            // Set the fist and last name separately because they are not part of the customer entity, but are saved in the generic attributes.

            updatedCustomer.FirstName = await _genericAttributeService.GetAttributeAsync<string>(currentCustomer, NopCustomerDefaults.FirstNameAttribute);
            updatedCustomer.LastName = await _genericAttributeService.GetAttributeAsync<string>(currentCustomer, NopCustomerDefaults.LastNameAttribute);
            updatedCustomer.LanguageId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.LanguageIdAttribute);
            updatedCustomer.CurrencyId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.CurrencyIdAttribute);

            //activity log
            await CustomerActivityService.InsertActivityAsync("UpdateCustomer", await LocalizationService.GetResourceAsync("ActivityLog.UpdateCustomer"), currentCustomer);

            var customersRootObject = new CustomersRootObject();

            customersRootObject.Customers.Add(updatedCustomer);

            var json = JsonFieldsSerializer.Serialize(customersRootObject, string.Empty);

            return new RawJsonActionResult(json);
        }

        [HttpPost]
        [Route("api/customers/{customerId}/updateAddress", Name = "UpdateAddress")]
        [AuthorizePermission("ManageCustomers", ignore: true)]
        [GetRequestsErrorInterceptorActionFilter]
        [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> UpdateAddress([FromRoute] int customerId, [FromBody] AddressDto newAddress)
        {

            // TODO: add address validation via model binder
            if (!await CheckPermissions(customerId))
            {
                AccessDenied();
            }
            var customer = await CustomerService.GetCustomerByIdAsync(customerId);
            if (customer is null)
            {
                return Error(HttpStatusCode.NotFound, "customerId", $"Customer with id {customerId} not found");
            }

            Address address = await _addressService.GetAddressByIdAsync(newAddress.Id);
            if (address is null)
            {
                return Error(HttpStatusCode.NotFound, "addressId", $"Address with id {newAddress.Id} not found");
            }

            IList<Address> customerAddresses = await CustomerService.GetAddressesByCustomerIdAsync(customerId);

            if (!customerAddresses.ToList().Exists(x => x.Id == newAddress.Id))
            {
                return Error(HttpStatusCode.BadRequest, "addressId", $"Address with id {newAddress.Id} does not belong to customer with id {customerId}");
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

            return Ok(newAddress);
        }

        [HttpDelete]
        [Route("/api/customers/{customerId}/addresses/{addressId}", Name = "DeleteAddress")]
        [AuthorizePermission("ManageCustomers", ignore: true)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), 422)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteAddress(
            [FromRoute] int customerId,
            [FromRoute] int addressId

        )
        {
            // TODO: add address validation via model binder
            if (!await CheckPermissions(customerId))
            {
                AccessDenied();
            }
            var customer = await CustomerService.GetCustomerByIdAsync(customerId);
            if (customer is null)
            {
                return Error(HttpStatusCode.NotFound, "customerId", $"Customer with id {customerId} not found");
            }

            Address address = await _addressService.GetAddressByIdAsync(addressId);
            if (address is null)
            {
                return Error(HttpStatusCode.NotFound, "addressId", $"Address with id {addressId} not found");
            }

            IList<Address> customerAddresses = await CustomerService.GetAddressesByCustomerIdAsync(customerId);

            if (!customerAddresses.ToList().Exists(x => x.Id == addressId))
            {
                return Error(HttpStatusCode.BadRequest, "addressId", $"Address with id {addressId} does not belong to customer with id {customerId}");
            }

            await CustomerService.RemoveCustomerAddressAsync(customer, address);

            return new RawJsonActionResult("{}");
        }


        [HttpDelete]
        [Route("/api/customers/{id}", Name = "DeleteCustomer")]
        [GetRequestsErrorInterceptorActionFilter]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteCustomer([FromRoute] int id)
        {
            if (id <= 0)
            {
                return Error(HttpStatusCode.BadRequest, "id", "invalid id");
            }

            var customer = await _customerApiService.GetCustomerEntityByIdAsync(id);

            if (customer == null)
            {
                return Error(HttpStatusCode.NotFound, "customer", "not found");
            }

            await CustomerService.DeleteCustomerAsync(customer);

            //remove newsletter subscription (if exists)
            foreach (var store in await StoreService.GetAllStoresAsync())
            {
                var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customer.Email, store.Id);
                if (subscription != null)
                {
                    await _newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(subscription);
                }
            }

            //activity log
            await CustomerActivityService.InsertActivityAsync("DeleteCustomer", await LocalizationService.GetResourceAsync("ActivityLog.DeleteCustomer"), customer);

            return new RawJsonActionResult("{}");
        }

        [HttpPost]
        [Route("api/customers/{customerId}/billingaddress", Name = "SetBillingAddress")]
        [AuthorizePermission("ManageCustomers", ignore: true)]
        [GetRequestsErrorInterceptorActionFilter]
        [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> SetBillingAddress([FromRoute] int customerId, [FromBody] AddressDto newAddress)
        {
            // TODO: add address validation via model binder
            if (!await CheckPermissions(customerId))
            {
                AccessDenied();
            }
            var customer = await CustomerService.GetCustomerByIdAsync(customerId);
            if (customer is null)
            {
                return Error(HttpStatusCode.NotFound, "customerId", $"Customer with id {customerId} not found");
            }
            var address = await InsertNewCustomerAddressIfDoesNotExist(customer, newAddress);
            customer.BillingAddressId = address.Id;
            await CustomerService.UpdateCustomerAsync(customer);
            return Ok(address.ToDto());
        }

        [HttpPost]
        [Route("api/customers/{customerId}/shippingaddress", Name = "SetShippingAddress")]
        [AuthorizePermission("ManageCustomers", ignore: true)]
        [GetRequestsErrorInterceptorActionFilter]
        [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorsRootObject), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> SetShippingAddress([FromRoute] int customerId, [FromBody] AddressDto newAddress)
        {
            // TODO: add address validation via model binder
            if (!await CheckPermissions(customerId))
            {
                AccessDenied();
            }
            var customer = await CustomerService.GetCustomerByIdAsync(customerId);
            if (customer is null)
            {
                return Error(HttpStatusCode.NotFound, "customerId", $"Customer with id {customerId} not found");
            }
            var address = await InsertNewCustomerAddressIfDoesNotExist(customer, newAddress);
            customer.ShippingAddressId = address.Id;
            await CustomerService.UpdateCustomerAsync(customer);
            return Ok(address.ToDto());
        }

        #region Private methods

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

        private async Task AddValidRolesAsync(Delta<CustomerDto> customerDelta, Customer currentCustomer)
        {
            var allCustomerRoles = await CustomerService.GetAllCustomerRolesAsync(true);
            foreach (var customerRole in allCustomerRoles)
            {
                if (customerDelta.Dto.RoleIds.Contains(customerRole.Id))
                {
                    //new role
                    if (!await CustomerService.IsInCustomerRoleAsync(currentCustomer, customerRole.Name))
                    {
                        await CustomerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping
                        {
                            CustomerId = currentCustomer.Id,
                            CustomerRoleId = customerRole.Id
                        });
                    }
                }
                else
                {
                    if (await CustomerService.IsInCustomerRoleAsync(currentCustomer, customerRole.Name))
                    {
                        await CustomerService.RemoveCustomerRoleMappingAsync(currentCustomer, customerRole);
                    }
                }
            }
        }

        private async Task PopulateAddressCountryNamesAsync(CustomerDto newCustomerDto)
        {
            foreach (var address in newCustomerDto.Addresses)
            {
                await SetCountryNameAsync(address);
            }

            if (newCustomerDto.BillingAddress != null)
            {
                await SetCountryNameAsync(newCustomerDto.BillingAddress);
            }

            if (newCustomerDto.ShippingAddress != null)
            {
                await SetCountryNameAsync(newCustomerDto.ShippingAddress);
            }
        }

        private async Task SetCountryNameAsync(AddressDto address)
        {
            if (string.IsNullOrEmpty(address.CountryName) && address.CountryId.HasValue)
            {
                var country = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
                address.CountryName = country.Name;
            }
        }

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
}
