using Nop.Core.Configuration;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.JSON.Serializers;
using System.Collections.Generic;
using System.Text.Json;

#nullable enable

namespace Nop.Plugin.Api.Domain;

public class ApiSettings : ISettings
{
    public bool EnableApi { get; set; } = true;

    public int TokenExpiryInDays { get; set; } = 0;

    public string EnabledRoles { get; set; } = JsonSerializer.Serialize(Constants.EnabledRolesDicDefault);

    public Dictionary<string, bool> EnabledRolesDic
    {
        get
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, bool>>(EnabledRoles);

            if (obj == null)
            {
                return Constants.EnabledRolesDicDefault;
            }

            return obj;
        }
    }

}
