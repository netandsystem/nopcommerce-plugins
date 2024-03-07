using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Api.DTO;
using Nop.Plugin.Api.DTOs.Orders;
using Nop.Plugin.Api.JSON.Serializers;
using Nop.Plugin.Api.Services;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Api.Controllers;

#nullable enable

[Route("api/invoices")]

public class InvoiceController : BaseSyncController<InvoiceDto>
{
    #region Attributes


    #endregion

    #region Ctr
    public InvoiceController(
        IInvoiceApiService invoiceApiService,
        IJsonFieldsSerializer jsonFieldsSerializer,
        IAclService aclService,
        ICustomerService customerService,
        IStoreMappingService storeMappingService,
        IStoreService storeService,
        IDiscountService discountService,
        ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IAuthenticationService authenticationService,
        IStoreContext storeContext
    ) :
    base(
        invoiceApiService,
        jsonFieldsSerializer,
        aclService,
        customerService,
        storeMappingService,
        storeService,
        discountService,
        customerActivityService,
        localizationService,
        pictureService,
        authenticationService,
        storeContext
    )
    {

    }


    #endregion

    #region Methods

    #endregion

    #region private Methods


    #endregion
}
