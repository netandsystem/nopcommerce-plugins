using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Banesco.Models
{
    public record ConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        [NopResourceDisplayName("Plugins.Payment.Banesco.DescriptionText")]
        public string? DescriptionText { get; set; }

        public int LanguageId { get; set; }
    }
}
