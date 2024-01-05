using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Api.Areas.Admin.Models;
using Nop.Plugin.Api.Domain;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using static Nop.Plugin.Api.Infrastructure.Constants;

namespace Nop.Plugin.Api.Areas.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class ApiAdminController : BasePluginController
    {
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        public ApiAdminController(
            IStoreContext storeContext,
            ISettingService settingService,
            ICustomerActivityService customerActivityService,
            ILocalizationService localizationService,
            INotificationService notificationService)
        {
            _storeContext = storeContext;
            _settingService = settingService;
            _customerActivityService = customerActivityService;
            _localizationService = localizationService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var apiSettings = await _settingService.LoadSettingAsync<ApiSettings>(storeScope);

            if (apiSettings.EnabledRolesDic.Count != System.Enum.GetValues(typeof(Roles)).Length)
            {
                // find missing roles
                var missingRoles = new List<string>();
                foreach (Roles role in System.Enum.GetValues(typeof(Roles)))
                {

                    if (!apiSettings.EnabledRolesDic.ContainsKey(role.ToString()))
                    {
                        missingRoles.Add(role.ToString());
                    }
                }

                var dic = apiSettings.EnabledRolesDic;

                // add missing roles
                foreach (var missingRole in missingRoles)
                {
                    dic.Add(missingRole, true);
                }

                apiSettings.EnabledRoles = System.Text.Json.JsonSerializer.Serialize(dic);
            }

            var model = apiSettings.ToModel();

            // Store Settings
            model.ActiveStoreScopeConfiguration = storeScope;

            if (model.EnableApi_OverrideForStore || storeScope == 0)
            {
                await _settingService.SaveSettingAsync(apiSettings, x => x.EnableApi, storeScope, false);
            }
            if (model.TokenExpiryInDays_OverrideForStore || storeScope == 0)
            {
                await _settingService.SaveSettingAsync(apiSettings, x => x.TokenExpiryInDays, storeScope, false);
            }

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            return View($"~/Plugins/Nop.Plugin.Api/Areas/Admin/Views/ApiAdmin/Settings.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(ConfigurationModel model)
        {
            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

            var settings = model.ToEntity();

            /* We do not clear cache after each setting update.
            * This behavior can increase performance because cached settings will not be cleared 
            * and loaded from database after each update */

            if (model.EnableApi_OverrideForStore || storeScope == 0)
            {
                await _settingService.SaveSettingAsync(settings, x => x.EnableApi, storeScope, false);
            }
            if (model.TokenExpiryInDays_OverrideForStore || storeScope == 0)
            {
                await _settingService.SaveSettingAsync(settings, x => x.TokenExpiryInDays, storeScope, false);
            }

            await _settingService.SaveSettingAsync(settings, x => x.EnabledRoles, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            await _customerActivityService.InsertActivityAsync("EditApiSettings", "Edit Api Settings");

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return View($"~/Plugins/Nop.Plugin.Api/Areas/Admin/Views/ApiAdmin/Settings.cshtml", model);
        }
    }
}
