using Nop.Plugin.Api.Infrastructure;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Plugin.Api.Areas.Admin.Models
{
    public class ConfigurationModel
    {
        [NopResourceDisplayName("Plugins.Api.Admin.EnableApi")]
        public bool EnableApi { get; set; }

        public bool EnableApi_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Api.Admin.TokenExpiryInDays")]
        public int TokenExpiryInDays { get; set; }

        public bool TokenExpiryInDays_OverrideForStore { get; set; }

        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Api.Admin.EnabledRoles")]
        public Dictionary<string, bool> EnabledRolesDic { get; set; } = Constants.EnabledRolesDicDefault;
    }
}
