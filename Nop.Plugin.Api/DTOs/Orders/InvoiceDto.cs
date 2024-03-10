using Nop.Plugin.Api.DTO.Base;
using Nop.Core.Domain.Orders;
using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Api.DTOs.Orders;

[JsonObject(Title = "invoice")]
public class InvoiceDto : BaseSyncDto
{
#nullable enable

    [JsonProperty("document_type")]
    public InvoiceType DocumentType { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }

    [JsonProperty("customer_id")]
    public int CustomerId { get; set; }

    [JsonProperty("customer_ext_id")]
    public string? CustomerExtId { get; set; }

    [JsonProperty("seller_id")]
    public int? SellerId { get; set; }

    [JsonProperty("customer_name")]
    public string? CustomerName { get; set; }

    [JsonProperty("balance")]
    public decimal Balance { get; set; }

    [JsonProperty("tax_printer_number")]
    public string? TaxPrinterNumber { get; set; }

    [JsonProperty("ext_id")]
    public string? ExtId { get; set; }
}