using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.ModelBinders;

namespace Nop.Plugin.Api.Models.OrdersParameters;

// JsonProperty is used only for swagger
[ModelBinder(typeof(ParametersModelBinder<OrdersParametersModel>))]
public class OrdersParametersModel : BaseOrdersParametersModel
{
    public OrdersParametersModel()
    {
        Limit = Constants.Configurations.DefaultLimit;
        Page = Constants.Configurations.DefaultPageValue;
        Fields = string.Empty;
        OrderByDateDesc = false;
    }

    /// <summary>
    ///     Amount of results (default: 50) (maximum: 250)
    /// </summary>
    [JsonProperty("limit")]
    public int Limit { get; set; }

    /// <summary>
    ///     Page to show (default: 1)
    /// </summary>
    [JsonProperty("page")]
    public int Page { get; set; }

    /// <summary>
    ///     comma-separated list of fields to include in the response
    /// </summary>
    [JsonProperty("fields")]
    public string Fields { get; set; }

    /// <summary>
    ///     Order by date
    /// </summary>
    [JsonProperty("order_by_date_desc")]
    public bool OrderByDateDesc { get; set; }

    /// <summary>
    ///     Order customer
    /// </summary>
    [JsonProperty("customer_id")]
    public int? CustomerId { get; set; }
}
