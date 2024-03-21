using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.ShoppingCarts;
using System;

#nullable enable

namespace Nop.Plugin.Api.DTO.Customers;

[JsonObject(Title = "customer")]
//[Validator(typeof(CustomerDtoValidator))]
public class CustomerDto : BaseSyncDto
{
    private ICollection<AddressDto>? _addresses;

    /// <summary>
    ///     Gets or sets the email
    /// </summary>
    [JsonProperty("user_name", Required = Required.Always)]
    public string Username { get; set; } = string.Empty;

    [JsonProperty("system_name")]
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing address identifier
    /// </summary>
    [JsonProperty("billing_address_id", Required = Required.AllowNull)]
    public int? BillingAddressId { get; set; }

    /// <summary>
    /// Gets or sets the vendor identifier with which this customer is associated (maganer)
    /// </summary>
    [JsonProperty("vendor_id")]
    public int VendorId { get; set; }

    /// <summary>
    /// Gets or sets the date and time of entity creation
    /// </summary>
    [JsonProperty("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    #region Navigation properties

    /// <summary>
    ///     Default billing address
    /// </summary>
    [JsonProperty("billing_address")]
    public AddressDto? BillingAddress { get; set; }

    /// <summary>
    ///     Default billing address
    /// </summary>
    [JsonProperty("shipping_address")]
    public AddressDto? ShippingAddress { get; set; }

    /// <summary>
    ///     Gets or sets customer addresses
    /// </summary>
    [JsonProperty("addresses")]
    public ICollection<AddressDto> Addresses
    {
        get
        {
            _addresses ??= new List<AddressDto>();

            return _addresses;
        }
        set => _addresses = value;
    }

    /// <summary>
    /// Gets or sets the email
    /// </summary>
    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the associated seller
    /// </summary>
    [JsonProperty("seller_id")]
    public int? SellerId { get; set; }


    [JsonProperty("first_name")]
    public string? FirstName { get; set; }
    [JsonProperty("last_name")]
    public string? LastName { get; set; }
    [JsonProperty("identity_card")]
    public string? IdentityCard { get; set; }

    [JsonProperty("phone")]
    public string? Phone { get; set; }

    [JsonProperty("balance")]
    public decimal Balance { get; set; }

    #endregion
}
