using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Requirements;

namespace Nop.Plugin.Api.Authorization.Policies;

#nullable enable

public abstract class BaseRoleAuthorizationPolicy<TCustomerRoleRequirement> : AuthorizationHandler<TCustomerRoleRequirement>
    where TCustomerRoleRequirement : BaseCustomerRoleRequirement
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, TCustomerRoleRequirement requirement)
    {
        if (await requirement.IsCustomerInRoleAsync())
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
