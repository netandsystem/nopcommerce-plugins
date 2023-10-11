using Nop.Core;
using Nop.Core.Domain.Configuration;
using Nop.Data;
using Nop.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LinqToDB.Reflection.Methods.LinqToDB.Insert;

namespace Template.Payments.Manual.Services;

public class TemplatePaymentService<USettings> where USettings : TemplatePaymentSettings, new()
{
    private readonly IStoreContext _storeContext;
    private readonly ISettingService _settingService;
    private readonly IRepository<Setting> _settingRepository;

    public TemplatePaymentService(IStoreContext storeContext, ISettingService settingService, IRepository<Setting> settingRepository)
    {
        _storeContext = storeContext;
        _settingService = settingService;
        _settingRepository = settingRepository;
    }

    public async Task<TemplatePaymentSettings> GetPaymentSettingsAsync()
    {
        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();

        var paymentSettings = await _settingService.LoadSettingAsync<USettings>(storeScope);

        return paymentSettings;
    }

    public async Task<string?> GetPaymentDescriptionTextAsync()
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var paymentSettings = await _settingService.LoadSettingAsync<USettings>(storeScope);
        return paymentSettings.DescriptionText;
    }

    public virtual async Task<IList<Setting>> GetAllSettingsByNameAsync(string keywords)
    {
        var settings = await _settingRepository.GetAllAsync(query =>
        {
            return from s in query
                   where s.Name.Contains(keywords)
                   select s;
        }, cache => default);

        return settings;
    }

    public virtual async Task<IList<Setting>> GetAllDescriptionTextSettingsAsync(string keywords)
    {
        return await GetAllSettingsByNameAsync("paymentsettings.descriptiontext");
    }


}
