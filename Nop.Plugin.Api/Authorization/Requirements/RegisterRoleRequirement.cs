using Nop.Plugin.Api.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Authorization.Requirements;

public class RegisterRoleRequirement : BaseCustomerRoleRequirement
{
    public RegisterRoleRequirement() : base(Constants.Roles.Registered.ToString())
    {
    }
}
