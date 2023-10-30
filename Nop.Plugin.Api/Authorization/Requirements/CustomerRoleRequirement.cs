using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MySqlX.XDevAPI.Common;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Plugin.Api.Domain;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Services.Configuration;
using Nop.Services.Customers;

namespace Nop.Plugin.Api.Authorization.Requirements;

#nullable enable

public class CustomerRoleRequirement : IAuthorizationRequirement
{
    private readonly string _roleName;
    private readonly Constants.Roles _roleEnum;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICustomerService _customerService;
    private readonly IStoreContext _storeContext;
    private readonly ISettingService _settingService;

    public CustomerRoleRequirement(Constants.Roles roleName)
    {
        _roleName = roleName.ToString();
        _httpContextAccessor = EngineContext.Current.Resolve<IHttpContextAccessor>();
        _customerService = EngineContext.Current.Resolve<ICustomerService>();
        _storeContext = EngineContext.Current.Resolve<IStoreContext>();
        _settingService = EngineContext.Current.Resolve<ISettingService>();
    }

    public async Task<bool> IsCustomerInRoleAsync()
    {
        try
        {

            var customerIdClaim = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.NameIdentifier);

            if (customerIdClaim != null && Guid.TryParse(customerIdClaim.Value, out var customerGuid))
            {

                var customer = await _customerService.GetCustomerByGuidAsync(customerGuid);
                if (customer == null)
                {
                    return false;
                }

                var customerRoles = await _customerService.GetCustomerRolesAsync(customer);
                var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
                var apiSettings = await _settingService.LoadSettingAsync<ApiSettings>(storeScope);

                bool isRoleEnabled = apiSettings.ToModel().EnabledRolesDic[_roleName];
                bool isCustomerInRole = customerRoles.FirstOrDefault(cr => cr.SystemName == _roleName) != null;

                return isRoleEnabled && isCustomerInRole;
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("ERROR ON SECURITY POLICY ", ex.Message);
        }

        return false;
    }
}
