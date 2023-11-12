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
public class CustomerDto: BaseDto
{
    private ICollection<AddressDto>? _addresses;

    /// <summary>
    ///     Gets or sets the email
    /// </summary>
    [JsonProperty("user_name", Required = Required.Always)]
    public string Username { get; set; } = string.Empty;

    [JsonProperty("first_name", Required = Required.Always)]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("last_name", Required = Required.Always)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vendor identifier with which this customer is associated (maganer)
    /// </summary>
    [JsonProperty("vendor_id")]
    public int VendorId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer has been deleted
    /// </summary>
    [JsonProperty("deleted")]
    public bool Deleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time of entity creation
    /// </summary>
    [JsonProperty("updated_on_utc")]
    public DateTime CreatedOnUtc { get; set; }

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
    /// get or set the custom attributes
    /// </summary>
    //[JsonProperty("custom_customer_attributes")]
    //public string CustomCustomerAttributes { get; set; }

    [JsonProperty("identity_card")]
    public string? IdentityCard { get; set; } = null;

    /// <summary>
    /// get or set the custom attributes
    /// </summary>
    [JsonProperty("phone")]
    public string? Phone { get; set; } = null;

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



    #endregion
}
