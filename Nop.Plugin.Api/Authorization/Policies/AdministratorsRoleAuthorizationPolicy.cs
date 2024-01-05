using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Nop.Plugin.Api.Authorization.Requirements;
using Nop.Plugin.Api.Infrastructure;

namespace Nop.Plugin.Api.Authorization.Policies;

public class AdministratorsRoleAuthorizationPolicy : BaseRoleAuthorizationPolicy
{
    public const string Name = nameof(AdministratorsRoleAuthorizationPolicy);
}
