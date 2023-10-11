using System.Collections.Generic;
using Newtonsoft.Json;
using Nop.Plugin.Api.Attributes;
using Nop.Plugin.Api.DTO.Base;
using Nop.Plugin.Api.DTO.ShoppingCarts;

#nullable enable

namespace Nop.Plugin.Api.DTO.Customers;

[JsonObject(Title = "customer")]
public class CustomerPost : BaseDto
{
    /// <summary>
    ///     Gets or sets the email
    /// </summary>
    [JsonProperty("user_name", Required = Required.Always)]
    public string Username { get; set; } = string.Empty;

    [JsonProperty("first_name", Required = Required.Always)]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("last_name", Required = Required.Always)]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("password", Required = Required.Always)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// get or set the custom attributes
    /// </summary>
    //[JsonProperty("custom_customer_attributes")]
    //public string CustomCustomerAttributes { get; set; }

    [JsonProperty("identity_card", Required = Required.Always)]
    public string? IdentityCard { get; set; } = null;

    /// <summary>
    /// get or set the custom attributes
    /// </summary>
    [JsonProperty("phone", Required = Required.Always)]
    public string? Phone { get; set; } = null;

}
