using Nop.Web.Framework.Mvc.ModelBinding;

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

        [NopResourceDisplayName("Plugins.Api.Admin.EnableClients")]
        public bool EnableClients { get; set; } = true;

        [NopResourceDisplayName("Plugins.Api.Admin.EnableSellers")]
        public bool EnableSellers { get; set; } = true;
    }
}
