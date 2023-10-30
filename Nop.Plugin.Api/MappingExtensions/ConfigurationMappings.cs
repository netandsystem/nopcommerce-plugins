using Nop.Plugin.Api.Areas.Admin.Models;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.Domain;
using Nop.Plugin.Api.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text.Json;

#nullable enable

namespace Nop.Plugin.Api.MappingExtensions;

public static class ConfigurationMappings
{
    public static ConfigurationModel ToModel(this ApiSettings apiSettings)
    {
        var newObj = apiSettings.MapTo<ApiSettings, ConfigurationModel>();

        Dictionary<string, bool>? dictionary;
        try
        {
            dictionary = JsonSerializer.Deserialize<Dictionary<string, bool>>(apiSettings.EnabledRoles);
        } catch
        {
            dictionary = new Dictionary<string, bool>() {
                { Constants.Roles.Registered.ToString(), true },
                { Constants.Roles.Seller.ToString(), true},
            };
        }

        newObj.EnabledRolesDic = dictionary;

        return newObj;
    }

    public static ApiSettings ToEntity(this ConfigurationModel apiSettingsModel)
    {
        var newObj = apiSettingsModel.MapTo<ConfigurationModel, ApiSettings>();
        string json = JsonSerializer.Serialize(apiSettingsModel.EnabledRolesDic);
        newObj.EnabledRoles = json;
        return newObj;
    }
}
