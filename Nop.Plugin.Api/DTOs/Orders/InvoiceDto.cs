using Nop.Plugin.Api.DTO.Base;
using Nop.Core.Domain.Orders;
using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Api.DTOs.Orders;

#nullable enable

[JsonObject(Title = "invoice")]
public class InvoiceDto : BaseSyncDto
{

    [JsonProperty("invoice_number")]
    public string InvoiceNumber { get; set; }

    [JsonProperty("document_type")]
    public DocumentType DocumentType { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }

    [JsonProperty("customer_id")]
    public int CustomerId { get; set; }

    [JsonProperty("seller_id")]
    public int SellerId { get; set; }

    [JsonProperty("customer_name")]
    public string? CustomerName { get; set; }

    [JsonProperty("balance")]
    public decimal Balance { get; set; }

    public InvoiceDto(string invoiceNumber, DocumentType documentType, DateTime createdOnUtc, decimal total, int customerId, int SellerId, string? customerName, decimal balance)
    {
        InvoiceNumber = invoiceNumber;
        DocumentType = documentType;
        CreatedOnUtc = createdOnUtc;
        Total = total;
        CustomerId = customerId;
        this.SellerId = SellerId;
        CustomerName = customerName;
        Balance = balance;
    }
}