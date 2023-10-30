using Nop.Core.Configuration;
using Nop.Plugin.Api.Infrastructure;
using System.Collections.Generic;

#nullable enable

namespace Nop.Plugin.Api.Domain;

public class ApiSettings : ISettings
{
    public bool EnableApi { get; set; } = true;

    public int TokenExpiryInDays { get; set; } = 0;

    public Dictionary<Constants.Roles, bool> EnabledRoles { get; } = new() {
        { Constants.Roles.Registered, true },
        { Constants.Roles.Seller, true},
    };
}
