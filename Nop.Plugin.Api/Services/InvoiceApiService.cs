using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTOs.Orders;
using Nop.Plugin.Api.MappingExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Services;

#nullable enable

public class InvoiceApiService : BaseSyncService<InvoiceDto>, IInvoiceApiService
{
    #region Fields

    private readonly IRepository<Invoice> _invoiceRepository;
    private readonly IRepository<Customer> _customerRepository;

    #endregion

    #region Ctor

    public InvoiceApiService(
        IRepository<Invoice> invoiceRepository,
        IRepository<Customer> customerRepository
    )
    {
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
    }

    #endregion

    #region Methods

    public override async Task<BaseSyncResponse> GetLastestUpdatedItems3Async(
       IList<int>? idsInDb, long? lastUpdateTs, int sellerId, int storeId
    )
    {
        async Task<List<InvoiceDto>> GetSellerItemsAsync()
        {
            var query = from i in _invoiceRepository.Table
                        where i.SellerId == sellerId
                        select i.ToDto();

            return await query.ToListAsync();
        }

        return await GetLastestUpdatedItems3Async(
            idsInDb,
            lastUpdateTs,
            GetSellerItemsAsync
         );
    }

    public override List<List<object?>> GetItemsCompressed3(IList<InvoiceDto> items)
    {
        /*
        [
            id, number
            deleted,  boolean
            updated_on_ts,  number
      
            ext_id,  string
            document_type, string
            total, number
            created_on_ts, number
            customer_name, string
            customer_id, number
            seller_id, number
            balance, number
            tax_printer_number, string
        ]
        */


        return items.Select(p =>
            new List<object?>()
            {
                p.Id,
                false,
                p.UpdatedOnTs,

                p.ExtId,
                p.DocumentType.ToString(),
                p.Total,
                p.CreatedOnTs,
                p.CustomerName,
                p.CustomerId,
                p.SellerId,
                p.Balance,
                p.TaxPrinterNumber
            }
        ).ToList();
    }


    #endregion

    #region Private Methods


    #endregion

    #region Private Classes

    #endregion
}
