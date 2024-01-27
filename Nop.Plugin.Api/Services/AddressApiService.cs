using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Data;
using Nop.Plugin.Api.DTO;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTOs.StateProvinces;
using Nop.Plugin.Api.MappingExtensions;
using Nop.Services.Customers;
using Nop.Services.Directory;

namespace Nop.Plugin.Api.Services;

public class AddressApiService : IAddressApiService
{
    #region Fields

    private readonly IStaticCacheManager _cacheManager;
    private readonly ICountryService _countryService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly IRepository<Address> _addressRepository;
    private readonly IRepository<CustomerAddressMapping> _customerAddressMappingRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly ICustomerApiService _customerApiService;
    private readonly ICustomerService _customerService;

    #endregion

    #region Ctor

    public AddressApiService(
        IRepository<Address> addressRepository,
        IRepository<CustomerAddressMapping> customerAddressMappingRepository,
        IStaticCacheManager staticCacheManager,
        ICountryService countryService,
        IStateProvinceService stateProvinceService,
        IRepository<Customer> customerRepository,
        ICustomerApiService customerApiService,
        ICustomerService customerService)
    {
        _addressRepository = addressRepository;
        _customerAddressMappingRepository = customerAddressMappingRepository;
        _cacheManager = staticCacheManager;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
        _customerRepository = customerRepository;
        _customerApiService = customerApiService;
        _customerService = customerService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a address mapped to customer
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="addressId">Address identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the result
    /// </returns>
    public async Task<AddressDto> GetCustomerAddressAsync(int customerId, int addressId)
    {
        var query = from address in _addressRepository.Table
                    join cam in _customerAddressMappingRepository.Table on address.Id equals cam.AddressId
                    where cam.CustomerId == customerId && address.Id == addressId
                    select address;

        var key = _cacheManager.PrepareKeyForShortTermCache(NopCustomerServicesDefaults.CustomerAddressCacheKey, customerId, addressId);

        var addressEntity = await _cacheManager.GetAsync(key, async () => await query.FirstOrDefaultAsync());
        return addressEntity?.ToDto();
    }

    public async Task<IList<CountryDto>> GetAllCountriesAsync(bool mustAllowBilling = false, bool mustAllowShipping = false)
    {
        IEnumerable<Country> countries = await _countryService.GetAllCountriesAsync();
        if (mustAllowBilling)
            countries = countries.Where(c => c.AllowsBilling);
        if (mustAllowShipping)
            countries = countries.Where(c => c.AllowsShipping);
        return countries.Select(c => c.ToDto()).ToList();
    }

    public async Task<CountryDto> GetCountryByIdAsync(int id)
    {
        var country = await _countryService.GetCountryByIdAsync(id);
        return country?.ToDto();
    }

    public async Task<IList<StateProvinceDto>> GetAllStateProvinceAsync()
    {
        IEnumerable<StateProvince> stateProvinces = await _stateProvinceService.GetStateProvincesAsync();
        return stateProvinces.Select(c => c.ToDto()).ToList();
    }

    public async Task<StateProvinceDto> GetStateProvinceByIdAsync(int id)
    {
        var province = await _stateProvinceService.GetStateProvinceByIdAsync(id);
        return province?.ToDto();
    }


    public async Task<AddressDto> GetAddressByIdAsync(int addressId)
    {
        var query = from address in _addressRepository.Table
                    where address.Id == addressId
                    select address;
        var addressEntity = await query.FirstOrDefaultAsync();
        return addressEntity?.ToDto();
    }

#nullable enable

    public async Task<BaseSyncResponse> GetLastestUpdatedItems2Async(
        IList<int>? idsInDb, DateTime? lastUpdateUtc, int sellerId
    )
    {
        var allCustomers = await _customerApiService.GetLastestUpdatedCustomersAsync(null, sellerId);

        var customersIds = allCustomers.Select(x => x.Id).ToList();

        var query = from address in _addressRepository.Table
                    join cam in _customerAddressMappingRepository.Table on address.Id equals cam.AddressId
                    where customersIds.Contains(cam.CustomerId)
                    select new CustomerAddress(cam.CustomerId, address.ToDto());

        /*  
            d = item in db
            s = item belongs to seller
            u = item updated after lastUpdateUtc

            s               // selected
            !d + u          // update o insert
            d!s             // delete
         
         */

        IList<int> _idsInDb = idsInDb ?? new List<int>();

        var selectedItems = await query.ToListAsync();
        var selectedItemsIds = selectedItems.Select(x => x.Address.Id).ToList();

        var itemsToInsertOrUpdate = selectedItems.Where(x =>
        {
            var d = _idsInDb.Contains(x.Address.Id);
            var u = lastUpdateUtc == null || x.Address.UpdatedOnUtc > lastUpdateUtc;

            return !d || u;
        }).ToList();


        var idsToDelete = _idsInDb.Where(x => !selectedItemsIds.Contains(x)).ToList();

        var itemsToSave = GetItemsCompressed(itemsToInsertOrUpdate);

        return new BaseSyncResponse(itemsToSave, idsToDelete);
    }

    #endregion

    #region Private Methods

    private List<List<object?>> GetItemsCompressed(IList<CustomerAddress> items)
    {
        /*
            [
              id, number
              deleted,  boolean
              updated_on_ts,  number
      
              address1,  string
              address2,  string
            ]
        */

        return items.Select(p =>
            new List<object?>()
            {
                p.Address.Id,
                false,
                p.Address.UpdatedOnTs,

                p.Address.Address1,
                p.Address.Address2,
            }
        ).ToList();
    }


    #endregion

    #region Private Classes

    private class CustomerAddress
    {
        public CustomerAddress(int customerId, AddressDto address)
        {
            CustomerId = customerId;
            Address = address;
        }

        public int CustomerId { get; set; }
        public AddressDto Address { get; set; }
    }

    #endregion
}
