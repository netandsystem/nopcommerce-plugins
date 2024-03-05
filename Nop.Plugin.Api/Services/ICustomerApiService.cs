using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTO.Products;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.Infrastructure;

namespace Nop.Plugin.Api.Services;

#nullable enable

public interface ICustomerApiService : IBaseSyncService<CustomerDto>
{
    Task<int> GetCustomersCountAsync();

    Task<Customer> GetCustomerEntityByIdAsync(int id);

    //Task<IList<CustomerDto>> GetCustomersDtosAsync(
    //    DateTime? createdAtMin = null, DateTime? createdAtMax = null,
    //    int limit = Constants.Configurations.DefaultLimit, int page = Constants.Configurations.DefaultPageValue,
    //    int sinceId = Constants.Configurations.DefaultSinceId);

    //Task<IList<CustomerDto>> SearchAsync(
    //    string query = "", string order = Constants.Configurations.DefaultOrder,
    //    int page = Constants.Configurations.DefaultPageValue, int limit = Constants.Configurations.DefaultLimit);

    Task<Dictionary<string, string>> GetFirstAndLastNameByCustomerIdAsync(int customerId);

    /// <summary>
    /// Gets current user working language
    /// </summary>
    Task<Language> GetCustomerLanguageAsync(Customer customer);

    /// <summary>
    /// Sets current user working language
    /// </summary>
    /// <param name="language">Language</param>
    Task SetCustomerLanguageAsync(Customer customer, Language language);

    /// <summary>
    /// Gets or sets current user working currency
    /// </summary>
    Task<Currency> GetCustomerCurrencyAsync(Customer customer);

    /// <summary>
    /// Sets current user working currency
    /// </summary>
    /// <param name="currency">Currency</param>
    Task SetCustomerCurrencyAsync(Customer customer, Currency currency);

    Task<Address?> GetCustomerAddressAsync(int customerId, int addressId);

    Task<List<CustomerDto>> GetLastestUpdatedCustomersAsync(
        DateTime? lastUpdateUtc, int? SellerId
    );

    Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int SellerId
    );

    Task<List<CustomerDto>> JoinCustomerDtosWithCustomerAttributesAsync(IList<CustomerDto> customers);

    Task<List<CustomerDto>> JoinCustomersWithAddressesAsync(List<Customer> customers);

    List<List<object?>> GetItemsCompressed(IList<CustomerDto> items);
}
