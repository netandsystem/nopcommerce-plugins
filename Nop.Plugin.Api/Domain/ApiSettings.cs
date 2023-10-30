using Nop.Core.Configuration;

namespace Nop.Plugin.Api.Domain
{
    public class ApiSettings : ISettings
    {
        public bool EnableApi { get; set; } = true;

        public int TokenExpiryInDays { get; set; } = 0;

        public bool EnableClients { get; set; } = true;

        public bool EnableSellers { get; set; } = true;
    }
}
