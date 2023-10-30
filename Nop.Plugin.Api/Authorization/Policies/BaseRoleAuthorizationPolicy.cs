﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Requirements;

namespace Nop.Plugin.Api.Authorization.Policies;

#nullable enable

public class BaseRoleAuthorizationPolicy : AuthorizationHandler<CustomerRoleRequirement>
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomerRoleRequirement requirement)
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
