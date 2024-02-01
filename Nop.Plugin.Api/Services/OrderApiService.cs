using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Api.DataStructures;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.DTO.Orders;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.MappingExtensions;
using System.Threading.Tasks;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Api.DTO.Products;
using Nop.Services.Orders;
using System.Text.Json;
using Nop.Services.Payments;
using Nop.Core.Domain.Stores;
using Nop.Services.Common;
using Nop.Services.Shipping;
using Nop.Services.Customers;
using Nop.Core;
using Nop.Services.Localization;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Nop.Plugin.Api.DTOs.Base;
using Nop.Plugin.Api.DTO.OrderItems;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Tax;
using System.Globalization;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;

namespace Nop.Plugin.Api.Services;

#nullable enable

public class OrderApiService : IOrderApiService
{
    #region Fields

    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<Address> _addressRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IProductApiService _productApiService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IPaymentService _paymentService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IShippingService _shippingService;
    private readonly ICustomerService _customerService;
    private readonly ICustomerApiService _customerApiService;
    private readonly ShippingSettings _shippingSettings;
    private readonly ILocalizationService _localizationService;
    private readonly IShoppingCartService _shoppingCartService;

    #endregion

    #region Ctr

    public OrderApiService(
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Address> addresspository,
        IRepository<Product> productRepository,
        IProductApiService productApiService,
        IOrderProcessingService orderProcessingService,
        IPaymentService paymentService,
        IGenericAttributeService genericAttributeService,
        IShippingService shippingService,
        ICustomerService customerService,
        ShippingSettings shippingSettings,
        ICustomerApiService customerApiService,
        ILocalizationService localizationService,
        IRepository<Customer> customerRepository,
        IShoppingCartService shoppingCartService
    )
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _addressRepository = addresspository;
        _productRepository = productRepository;
        _productApiService = productApiService;
        _orderProcessingService = orderProcessingService;
        _paymentService = paymentService;
        _genericAttributeService = genericAttributeService;
        _shippingService = shippingService;
        _customerService = customerService;
        _shippingSettings = shippingSettings;
        _customerApiService = customerApiService;
        _localizationService = localizationService;
        _customerRepository = customerRepository;
        _shoppingCartService = shoppingCartService;
    }

    #endregion

    #region Methods

    public async Task<List<OrderDto>> GetOrders(
        int? customerId,
        int? limit,
        int? page,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        ShippingStatus? shippingStatus,
        int? storeId,
        bool orderByDateDesc,
        DateTime? createdAtMin,
        DateTime? createdAtMax,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    )
    {
        int limitValue = limit ?? Constants.Configurations.DefaultLimit;
        int pageValue = page ?? Constants.Configurations.DefaultPageValue;

        var ordersQuery = GetOrdersQuery(
                customerId: customerId,
                createdAtMin: createdAtMin,
                createdAtMax: createdAtMax,
                status: status,
                paymentStatus: paymentStatus,
                shippingStatus: shippingStatus,
                storeId: storeId,
                orderByDateDesc: orderByDateDesc,
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );

        var ordersItemQuery = from order in ordersQuery
                              join address in _addressRepository.Table
                              on order.BillingAddressId equals address.Id
                              join orderItem in _orderItemRepository.Table
                              on order.Id equals orderItem.OrderId
                              into orderItemsGroup
                              select order.ToDto(orderItemsGroup.ToList(), address, _paymentService.DeserializeCustomValues(order), null);

        var apiList = new ApiList<OrderDto>(ordersItemQuery, pageValue - 1, limitValue);

        var ordersDto = await apiList.ToListAsync();

        HashSet<int> productIds = new();

        foreach (var order in ordersDto)
        {
            var ids = order.OrderItems.Select(item => item.ProductId);

            foreach (var id in ids)
            {
                productIds.Add(id);
            }
        }

        var productsQuery = from product in _productRepository.Table
                            where productIds.Any(id => id == product.Id)
                            select product;

        var products = await productsQuery.ToListAsync();

        var productsDto = await _productApiService.JoinProductsAndPicturesAsync(products);

        foreach (var order in ordersDto)
        {
            foreach (var item in order.OrderItems)
            {
                item.Product = productsDto.Find(p => p.Id == item.ProductId);

                if (item.Product is null)
                {
                    throw new Exception("There are some products null in GetOrders");
                }
            }
        }

        return ordersDto;
    }

    //public async Task<List<OrderPost>> FilterNotCreatedOrdersPostAsync(IList<OrderPost> ordersPost)
    //{
    //    var query = from order in _orderRepository.Table
    //                where !ordersPost.Contains(x => x.) && order.Deleted == false
    //                select order.OrderGuid;

    //}

    public async Task<PlaceOrderResult> PlaceOrderAsync(OrderPost newOrder, Customer customer, int storeId, IList<ShoppingCartItem> cart)
    {
        newOrder.CustomValuesXml ??= new();

        if (newOrder.PaymentData is not null)
        {
            newOrder.CustomValuesXml.Add("Número de referencia", newOrder.PaymentData.ReferenceNumber);
            newOrder.CustomValuesXml.Add("Monto en Bs", newOrder.PaymentData.AmountInBs);
        }

        bool pickupInStore = newOrder.PickUpInStore ?? false;

        if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            if (pickupInStore && _shippingSettings.AllowPickupInStore)
            {
                //pickup point
                var response = await _shippingService.GetPickupPointsAsync(customer.BillingAddressId ?? 0,
                customer, "", storeId);

                if (!response.Success)
                {
                    return new PlaceOrderResult
                    {
                        Errors = response.Errors
                    };
                }

                var selectedPoint = response.PickupPoints.FirstOrDefault();

                await SavePickupOptionAsync(selectedPoint, customer, storeId);
            }
            else
            {
                if (newOrder.ShippingRateComputationMethodSystemName is null)
                {
                    throw new Exception("if pick_up_in_store is false then shipping_rate_computation_method_system_name cannot be null");
                }

                if (newOrder.ShippingMethod is null)
                {
                    throw new Exception("if pick_up_in_store is false then shipping_method cannot be null");
                }

                //set value indicating that "pick up in store" option has not been chosen
#nullable disable
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, storeId);
#nullable enable

                //find shipping method
                //performance optimization. try cache first
                var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(customer,
                    NopCustomerDefaults.OfferedShippingOptionsAttribute, storeId);
                if (shippingOptions == null || !shippingOptions.Any())
                {
                    var address = await _customerService.GetCustomerShippingAddressAsync(customer);
                    //not found? let's load them using shipping service
                    shippingOptions = (await _shippingService.GetShippingOptionsAsync(cart, address,
                        customer, newOrder.ShippingRateComputationMethodSystemName, storeId)).ShippingOptions.ToList();
                }
                else
                {
                    //loaded cached results. let's filter result by a chosen shipping rate computation method
                    shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(newOrder.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                var shippingOption = shippingOptions
                    .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(newOrder.ShippingMethod, StringComparison.InvariantCultureIgnoreCase));

                if (shippingOption == null)
                {
                    throw new Exception("shipping method not found");
                }

                //save
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, storeId);
            }

        }


        var processPaymentRequest = new ProcessPaymentRequest
        {
            StoreId = storeId,
            CustomerId = customer.Id,
            PaymentMethodSystemName = newOrder.PaymentMethodSystemName,
            OrderGuid = newOrder.OrderGuid ?? Guid.NewGuid(),
            OrderGuidGeneratedOnUtc = DateTime.UtcNow,
            CustomValues = newOrder.CustomValuesXml,
            OrderManagerGuid = newOrder.OrderManagerGuid,
            SellerId = newOrder.SellerId
        };

        //_paymentService.GenerateOrderGuid(processPaymentRequest);

        var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);

        if (placeOrderResult.Success)
        {
            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = placeOrderResult.PlacedOrder
            };

            await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);
        }

        return placeOrderResult;
    }

    public async Task<List<OrderDto>> GetLastestUpdatedItemsAsync(DateTime? lastUpdateUtc, int sellerId, int storeId)
    {
        return await GetOrders(
                customerId: null,
                limit: null,
                page: null,
                status: null,
                paymentStatus: null,
                shippingStatus: null,
                storeId: storeId,
                orderByDateDesc: false,
                createdAtMin: null,
                createdAtMax: null,
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );
    }


    public async Task<BaseSyncResponse> GetLastestUpdatedItems2Async(DateTime? lastUpdateUtc, int sellerId, int storeId)
    {
        // get date 12 months ago
        //var createdAtMin = DateTime.UtcNow.AddMonths(-12);

        var items = await GetLastedUpdatedOrders(lastUpdateUtc, sellerId);

        var itemsCompressed = GetItemsCompressed(items);

        return new BaseSyncResponse(itemsCompressed);
    }

    public async Task<List<OrderDto>> GetLastedUpdatedOrders(
        DateTime? lastUpdateUtc,
        int sellerId
    )
    {
        var ordersQuery = GetOrdersQuery(
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );

        var ordersDtoQuery = from order in ordersQuery
                             join customer in _customerRepository.Table
                             on order.CustomerId equals customer.Id
                             join address in _addressRepository.Table
                             on order.BillingAddressId equals address.Id
                             select order.ToDto(new List<OrderItem>(), address, _paymentService.DeserializeCustomValues(order), customer);

        var ordersDto = await ordersDtoQuery.ToListAsync();

        var customers = ordersDto.Select(x => x.Customer).ToList();

        await _customerApiService.JoinCustomerDtosWithCustomerAttributesAsync(customers);

        return ordersDto;
    }


    public async Task<List<OrderDto>> GetOrders2(
        int? customerId,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        ShippingStatus? shippingStatus,
        int? storeId,
        bool orderByDateDesc,
        DateTime? createdAtMin,
        DateTime? createdAtMax,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    )
    {
        var ordersQuery = GetOrdersQuery(
                customerId: customerId,
                createdAtMin: createdAtMin,
                createdAtMax: createdAtMax,
                status: status,
                paymentStatus: paymentStatus,
                shippingStatus: shippingStatus,
                storeId: storeId,
                orderByDateDesc: orderByDateDesc,
                sellerId: sellerId,
                lastUpdateUtc: lastUpdateUtc
            );

        var ordersItemQuery = from order in ordersQuery
                              join customer in _customerRepository.Table
                              on order.CustomerId equals customer.Id
                              join address in _addressRepository.Table
                              on order.BillingAddressId equals address.Id
                              join orderItem in _orderItemRepository.Table
                              on order.Id equals orderItem.OrderId
                              into orderItemsGroup
                              select order.ToDto(orderItemsGroup.ToList(), address, _paymentService.DeserializeCustomValues(order), customer);


        var ordersDto = await ordersItemQuery.ToListAsync();

        HashSet<int> productIds = new();

        foreach (var order in ordersDto)
        {
            var ids = order.OrderItems.Select(item => item.ProductId);

            foreach (var id in ids)
            {
                productIds.Add(id);
            }
        }

        var productsQuery = from product in _productRepository.Table
                            where productIds.Any(id => id == product.Id)
                            select product.ToDto(null);

        var productsDto = await productsQuery.ToListAsync();

        var customersDto = ordersDto.Select(o => o.Customer).ToList();

        await _customerApiService.JoinCustomerDtosWithCustomerAttributesAsync(customersDto);

        foreach (var order in ordersDto)
        {
            foreach (var item in order.OrderItems)
            {
                item.Product = productsDto.Find(p => p.Id == item.ProductId);

                if (item.Product is null)
                {
                    throw new Exception("There are some products null in GetOrders");
                }
            }
        }

        return ordersDto;
    }

    public List<List<object?>> GetItemsCompressed(IList<OrderDto> items)
    {
        /*
        [
          id,   string
          deleted,  boolean
          updated_on_ts,  number

          created_on_ts,  number

          order_shipping_excl_tax,  number
          order_discount,  number
          custom_values,  json
      
          order_status,  string
          paid_date_ts,  number

          customer_id,  number
          customer_code: z.string().optional().nullable(),
          customer_business_name: z.string().optional().nullable(),
          customer_rif: z.string().optional().nullable(),

          billing_address_1: z.string().optional().nullable(),
          billing_address_2: z.string().optional().nullable(),
        ]
      */

        return items.Select(p =>
            new List<object?>() {
                p.Id,
                p.Deleted,
                p.UpdatedOnTs,

                p.CreatedOnTs,

                p.OrderShippingExclTax,
                p.OrderDiscount,
                p.CustomValues is null || p.CustomValues.Count == 0 ? null : p.CustomValues,

                p.OrderStatus,
                p.PaidDateTs,

                p.CustomerId,
                p.Customer?.SystemName,
                p.Customer?.Attributes?.GetValueOrDefault("company"),
                p.Customer?.Attributes?.GetValueOrDefault("rif"),

                p.BillingAddress.Address1,
                p.BillingAddress.Address2,
            }
        ).ToList();
    }


    /*

    public Order GetOrderById(int orderId)
    {
        if (orderId <= 0)
        {
            return null;
        }

        return _orderRepository.Table.FirstOrDefault(order => order.Id == orderId && !order.Deleted);
    }

    public int GetOrdersCount(
        DateTime? createdAtMin = null, DateTime? createdAtMax = null, OrderStatus? status = null,
        PaymentStatus? paymentStatus = null, ShippingStatus? shippingStatus = null,
        int? customerId = null, int? storeId = null)
    {
        var query = GetOrdersQuery(createdAtMin, createdAtMax, status, paymentStatus, shippingStatus, customerId: customerId, storeId: storeId);

        return query.Count();
    }

    */

    #endregion

    #region Private methods
    private IQueryable<Order> GetOrdersQuery(
        int? customerId = null,
        DateTime? createdAtMin = null,
        DateTime? createdAtMax = null,
        OrderStatus? status = null,
        PaymentStatus? paymentStatus = null,
        ShippingStatus? shippingStatus = null,
        int? storeId = null,
        bool orderByDateDesc = false,
        int? sellerId = null,
        DateTime? lastUpdateUtc = null
    )
    {
        var query = _orderRepository.Table;

        if (customerId != null)
        {
            query = query.Where(order => order.CustomerId == customerId);
        }

        if (status != null)
        {
            query = query.Where(order => order.OrderStatusId == (int)status);
        }

        if (paymentStatus != null)
        {
            query = query.Where(order => order.PaymentStatusId == (int)paymentStatus);
        }

        if (shippingStatus != null)
        {
            query = query.Where(order => order.ShippingStatusId == (int)shippingStatus);
        }

        query = query.Where(order => !order.Deleted);

        if (createdAtMin != null)
        {
            query = query.Where(order => order.CreatedOnUtc > createdAtMin.Value.ToUniversalTime());
        }

        if (createdAtMax != null)
        {
            query = query.Where(order => order.CreatedOnUtc < createdAtMax.Value.ToUniversalTime());
        }

        if (storeId != null)
        {
            query = query.Where(order => order.StoreId == storeId);
        }

        if (orderByDateDesc)
        {
            query = query.OrderByDescending(order => order.CreatedOnUtc);
        }
        else
        {
            query = query.OrderBy(order => order.Id);
        }

        if (sellerId != null)
        {
            query = query.Where(order => order.SellerId == sellerId);
        }

        if (lastUpdateUtc != null)
        {
            query = query.Where(order => order.UpdatedOnUtc > lastUpdateUtc);
        }

        return query;
    }

    private async Task SavePickupOptionAsync(PickupPoint? pickupPoint, Customer customer, int storeId)
    {
        if (pickupPoint == null)
        {
            throw new ArgumentNullException(nameof(pickupPoint));
        }

        var name = !string.IsNullOrEmpty(pickupPoint.Name) ?
            string.Format(await _localizationService.GetResourceAsync("Checkout.PickupPoints.Name"), pickupPoint.Name) :
            await _localizationService.GetResourceAsync("Checkout.PickupPoints.NullName");
        var pickUpInStoreShippingOption = new ShippingOption
        {
            Name = name,
            Rate = pickupPoint.PickupFee,
            Description = pickupPoint.Description,
            ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
            IsPickupInStore = true
        };

        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, storeId);
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, storeId);
    }

    //private async Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest)
    //{
    //    //1 . Prepare order details
    //    // customer, 
    //    //customer currency
    //    //customer language
    //}


    //protected virtual async Task<PlaceOrderContainer> PreparePlaceOrderDetailsAsync(ProcessPaymentRequest processPaymentRequest)
    //{
    //    var details = new PlaceOrderContainer
    //    {
    //        //customer
    //        Customer = await _customerService.GetCustomerByIdAsync(processPaymentRequest.CustomerId)
    //    };

    //    if (details.Customer == null)
    //        throw new ArgumentException("Customer is not set");

    //    //check whether customer is guest
    //    if (await _customerService.IsGuestAsync(details.Customer) && !_orderSettings.AnonymousCheckoutAllowed)
    //        throw new NopException("Anonymous checkout is not allowed");

    //    //customer currency
    //    var currencyTmp = await _currencyService.GetCurrencyByIdAsync(
    //        await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.CurrencyIdAttribute, processPaymentRequest.StoreId));
    //    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
    //    var customerCurrency = currencyTmp != null && currencyTmp.Published ? currencyTmp : currentCurrency;
    //    var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
    //    details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
    //    details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

    //    //customer language
    //    details.CustomerLanguage = await _languageService.GetLanguageByIdAsync(
    //        await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.LanguageIdAttribute, processPaymentRequest.StoreId));
    //    if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
    //        details.CustomerLanguage = await _workContext.GetWorkingLanguageAsync();

    //    //billing address
    //    if (details.Customer.BillingAddressId is null)
    //        throw new NopException("Billing address is not provided");

    //    var billingAddress = await _customerService.GetCustomerBillingAddressAsync(details.Customer);

    //    if (!CommonHelper.IsValidEmail(billingAddress?.Email))
    //        throw new NopException("Email is not valid");

    //    details.BillingAddress = _addressService.CloneAddress(billingAddress);

    //    //checkout attributes
    //    details.CheckoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.CheckoutAttributes, processPaymentRequest.StoreId);
    //    details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributesAsync(details.CheckoutAttributesXml, details.Customer);

    //    //tax display type
    //    if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
    //        details.CustomerTaxDisplayType = (TaxDisplayType)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.TaxDisplayTypeIdAttribute, processPaymentRequest.StoreId);
    //    else
    //        details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

    //    //sub total (incl tax)
    //    details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
    //    details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;


    //    //sub total (excl tax)
    //    details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
    //    details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

    //    //shipping info
    //    if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(details.Cart))
    //    {
    //        var pickupPoint = await _genericAttributeService.GetAttributeAsync<PickupPoint>(details.Customer,
    //            NopCustomerDefaults.SelectedPickupPointAttribute, processPaymentRequest.StoreId);
    //        if (_shippingSettings.AllowPickupInStore && pickupPoint != null)
    //        {
    //            var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(pickupPoint.CountryCode);
    //            var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(pickupPoint.StateAbbreviation, country?.Id);

    //            details.PickupInStore = true;
    //            details.PickupAddress = new Address
    //            {
    //                Address1 = pickupPoint.Address,
    //                City = pickupPoint.City,
    //                County = pickupPoint.County,
    //                CountryId = country?.Id,
    //                StateProvinceId = state?.Id,
    //                ZipPostalCode = pickupPoint.ZipPostalCode,
    //                CreatedOnUtc = DateTime.UtcNow
    //            };
    //        }
    //        else
    //        {
    //            if (details.Customer.ShippingAddressId == null)
    //                throw new NopException("Shipping address is not provided");

    //            var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(details.Customer);

    //            if (!CommonHelper.IsValidEmail(shippingAddress?.Email))
    //                throw new NopException("Email is not valid");

    //            //clone shipping address
    //            details.ShippingAddress = _addressService.CloneAddress(shippingAddress);

    //            if (await _countryService.GetCountryByAddressAsync(details.ShippingAddress) is Country shippingCountry && !shippingCountry.AllowsShipping)
    //                throw new NopException($"Country '{shippingCountry.Name}' is not allowed for shipping");
    //        }

    //        var shippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(details.Customer,
    //            NopCustomerDefaults.SelectedShippingOptionAttribute, processPaymentRequest.StoreId);
    //        if (shippingOption != null)
    //        {
    //            details.ShippingMethodName = shippingOption.Name;
    //            details.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
    //        }

    //        details.ShippingStatus = ShippingStatus.NotYetShipped;
    //    }
    //    else
    //        details.ShippingStatus = ShippingStatus.ShippingNotRequired;

    //    //shipping total
    //    var (orderShippingTotalInclTax, _, shippingTotalDiscounts) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, true);
    //    var (orderShippingTotalExclTax, _, _) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, false);
    //    if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
    //        throw new NopException("Shipping total couldn't be calculated");

    //    details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
    //    details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

    //    foreach (var disc in shippingTotalDiscounts)
    //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
    //            details.AppliedDiscounts.Add(disc);

    //    //payment total
    //    var paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFeeAsync(details.Cart, processPaymentRequest.PaymentMethodSystemName);
    //    details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, true, details.Customer)).price;
    //    details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, false, details.Customer)).price;

    //    //tax amount
    //    SortedDictionary<decimal, decimal> taxRatesDictionary;
    //    (details.OrderTaxTotal, taxRatesDictionary) = await _orderTotalCalculationService.GetTaxTotalAsync(details.Cart);

    //    //VAT number
    //    var customerVatStatus = (VatNumberStatus)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.VatNumberStatusIdAttribute);
    //    if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
    //        details.VatNumber = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.VatNumberAttribute);

    //    //tax rates
    //    details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
    //        $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

    //    //order total (and applied discounts, gift cards, reward points)
    //    var (orderTotal, orderDiscountAmount, orderAppliedDiscounts, appliedGiftCards, redeemedRewardPoints, redeemedRewardPointsAmount) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(details.Cart);
    //    if (!orderTotal.HasValue)
    //        throw new NopException("Order total couldn't be calculated");

    //    details.OrderDiscountAmount = orderDiscountAmount;
    //    details.RedeemedRewardPoints = redeemedRewardPoints;
    //    details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
    //    details.AppliedGiftCards = appliedGiftCards;
    //    details.OrderTotal = orderTotal.Value;

    //    //discount history
    //    foreach (var disc in orderAppliedDiscounts)
    //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
    //            details.AppliedDiscounts.Add(disc);

    //    processPaymentRequest.OrderTotal = details.OrderTotal;

    //    //recurring or standard shopping cart?
    //    details.IsRecurringShoppingCart = await _shoppingCartService.ShoppingCartIsRecurringAsync(details.Cart);
    //    if (!details.IsRecurringShoppingCart)
    //        return details;

    //    var (recurringCyclesError, recurringCycleLength, recurringCyclePeriod, recurringTotalCycles) = await _shoppingCartService.GetRecurringCycleInfoAsync(details.Cart);

    //    if (!string.IsNullOrEmpty(recurringCyclesError))
    //        throw new NopException(recurringCyclesError);

    //    processPaymentRequest.RecurringCycleLength = recurringCycleLength;
    //    processPaymentRequest.RecurringCyclePeriod = recurringCyclePeriod;
    //    processPaymentRequest.RecurringTotalCycles = recurringTotalCycles;

    //    return details;
    //}



    ///// <summary>
    ///// Places an order
    ///// </summary>
    ///// <param name="processPaymentRequest">Process payment request</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the place order result
    ///// </returns>
    //public virtual async Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest)
    //{
    //    //if (processPaymentRequest == null)
    //    //    throw new ArgumentNullException(nameof(processPaymentRequest));

    //    var result = new PlaceOrderResult();
    //    try
    //    {
    //        if (processPaymentRequest.OrderGuid == Guid.Empty)
    //            throw new Exception("Order GUID is not generated");

    //        //prepare order details
    //        var details = await PreparePlaceOrderDetailsAsync(processPaymentRequest);

    //        var processPaymentResult = await GetProcessPaymentResultAsync(processPaymentRequest, details);

    //        if (processPaymentResult == null)
    //            throw new NopException("processPaymentResult is not available");

    //        if (processPaymentResult.Success)
    //        {
    //            var order = await SaveOrderDetailsAsync(processPaymentRequest, processPaymentResult, details);
    //            result.PlacedOrder = order;

    //            //move shopping cart items to order items
    //            await MoveShoppingCartItemsToOrderItemsAsync(details, order);

    //            //discount usage history
    //            await SaveDiscountUsageHistoryAsync(details, order);

    //            //gift card usage history
    //            await SaveGiftCardUsageHistoryAsync(details, order);

    //            //recurring orders
    //            if (details.IsRecurringShoppingCart)
    //                await CreateFirstRecurringPaymentAsync(processPaymentRequest, order);

    //            //notifications
    //            await SendNotificationsAndSaveNotesAsync(order);

    //            //reset checkout data
    //            await _customerService.ResetCheckoutDataAsync(details.Customer, processPaymentRequest.StoreId, clearCouponCodes: true, clearCheckoutAttributes: true);
    //            await _customerActivityService.InsertActivityAsync("PublicStore.PlaceOrder",
    //                string.Format(await _localizationService.GetResourceAsync("ActivityLog.PublicStore.PlaceOrder"), order.Id), order);

    //            //check order status
    //            await CheckOrderStatusAsync(order);

    //            //raise event       
    //            await _eventPublisher.PublishAsync(new OrderPlacedEvent(order));

    //            if (order.PaymentStatus == PaymentStatus.Paid)
    //                await ProcessOrderPaidAsync(order);
    //        }
    //        else
    //            foreach (var paymentError in processPaymentResult.Errors)
    //                result.AddError(string.Format(await _localizationService.GetResourceAsync("Checkout.PaymentError"), paymentError));
    //    }
    //    catch (Exception exc)
    //    {
    //        await _logger.ErrorAsync(exc.Message, exc);
    //        result.AddError(exc.Message);
    //    }

    //    if (result.Success)
    //        return result;

    //    //log errors
    //    var logError = result.Errors.Aggregate("Error while placing order. ",
    //        (current, next) => $"{current}Error {result.Errors.IndexOf(next) + 1}: {next}. ");
    //    var customer = await _customerService.GetCustomerByIdAsync(processPaymentRequest.CustomerId);
    //    await _logger.ErrorAsync(logError, customer: customer);

    //    return result;
    //}


    ///// <summary>
    ///// Prepare details to place an order. It also sets some properties to "processPaymentRequest"
    ///// </summary>
    ///// <param name="processPaymentRequest">Process payment request</param>
    ///// <returns>
    ///// A task that represents the asynchronous operation
    ///// The task result contains the details
    ///// </returns>
    //protected virtual async Task<PlaceOrderContainer> PreparePlaceOrderDetailsAsync(ProcessPaymentRequest processPaymentRequest)
    //{
    //    var details = new PlaceOrderContainer
    //    {
    //        //customer
    //        Customer = await _customerService.GetCustomerByIdAsync(processPaymentRequest.CustomerId)
    //    };
    //    if (details.Customer == null)
    //        throw new ArgumentException("Customer is not set");

    //    //affiliate
    //    var affiliate = await _affiliateService.GetAffiliateByIdAsync(details.Customer.AffiliateId);
    //    if (affiliate != null && affiliate.Active && !affiliate.Deleted)
    //        details.AffiliateId = affiliate.Id;

    //    //check whether customer is guest
    //    if (await _customerService.IsGuestAsync(details.Customer) && !_orderSettings.AnonymousCheckoutAllowed)
    //        throw new NopException("Anonymous checkout is not allowed");

    //    //customer currency
    //    var currencyTmp = await _currencyService.GetCurrencyByIdAsync(
    //        await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.CurrencyIdAttribute, processPaymentRequest.StoreId));
    //    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
    //    var customerCurrency = currencyTmp != null && currencyTmp.Published ? currencyTmp : currentCurrency;
    //    var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
    //    details.CustomerCurrencyCode = customerCurrency.CurrencyCode;
    //    details.CustomerCurrencyRate = customerCurrency.Rate / primaryStoreCurrency.Rate;

    //    //customer language
    //    details.CustomerLanguage = await _languageService.GetLanguageByIdAsync(
    //        await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.LanguageIdAttribute, processPaymentRequest.StoreId));
    //    if (details.CustomerLanguage == null || !details.CustomerLanguage.Published)
    //        details.CustomerLanguage = await _workContext.GetWorkingLanguageAsync();

    //    //billing address
    //    if (details.Customer.BillingAddressId is null)
    //        throw new NopException("Billing address is not provided");

    //    var billingAddress = await _customerService.GetCustomerBillingAddressAsync(details.Customer);

    //    if (!CommonHelper.IsValidEmail(billingAddress?.Email))
    //        throw new NopException("Email is not valid");

    //    details.BillingAddress = _addressService.CloneAddress(billingAddress);

    //    if (await _countryService.GetCountryByAddressAsync(details.BillingAddress) is Country billingCountry && !billingCountry.AllowsBilling)
    //        throw new NopException($"Country '{billingCountry.Name}' is not allowed for billing");

    //    //checkout attributes
    //    details.CheckoutAttributesXml = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.CheckoutAttributes, processPaymentRequest.StoreId);
    //    details.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributesAsync(details.CheckoutAttributesXml, details.Customer);

    //    //load shopping cart
    //    details.Cart = await _shoppingCartService.GetShoppingCartAsync(details.Customer, ShoppingCartType.ShoppingCart, processPaymentRequest.StoreId);

    //    if (!details.Cart.Any())
    //        throw new NopException("Cart is empty");

    //    //validate the entire shopping cart
    //    var warnings = await _shoppingCartService.GetShoppingCartWarningsAsync(details.Cart, details.CheckoutAttributesXml, true);
    //    if (warnings.Any())
    //        throw new NopException(warnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));

    //    //validate individual cart items
    //    foreach (var sci in details.Cart)
    //    {
    //        var product = await _productService.GetProductByIdAsync(sci.ProductId);

    //        var sciWarnings = await _shoppingCartService.GetShoppingCartItemWarningsAsync(details.Customer,
    //            sci.ShoppingCartType, product, processPaymentRequest.StoreId, sci.AttributesXml,
    //            sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false, sci.Id);
    //        if (sciWarnings.Any())
    //            throw new NopException(sciWarnings.Aggregate(string.Empty, (current, next) => $"{current}{next};"));
    //    }

    //    //min totals validation
    //    if (!await ValidateMinOrderSubtotalAmountAsync(details.Cart))
    //    {
    //        var minOrderSubtotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderSubtotalAmount, currentCurrency);
    //        throw new NopException(string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderSubtotalAmount"),
    //            await _priceFormatter.FormatPriceAsync(minOrderSubtotalAmount, true, false)));
    //    }

    //    if (!await ValidateMinOrderTotalAmountAsync(details.Cart))
    //    {
    //        var minOrderTotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderTotalAmount, currentCurrency);
    //        throw new NopException(string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderTotalAmount"),
    //            await _priceFormatter.FormatPriceAsync(minOrderTotalAmount, true, false)));
    //    }

    //    //tax display type
    //    if (_taxSettings.AllowCustomersToSelectTaxDisplayType)
    //        details.CustomerTaxDisplayType = (TaxDisplayType)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.TaxDisplayTypeIdAttribute, processPaymentRequest.StoreId);
    //    else
    //        details.CustomerTaxDisplayType = _taxSettings.TaxDisplayType;

    //    //sub total (incl tax)
    //    var (orderSubTotalDiscountAmount, orderSubTotalAppliedDiscounts, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, true);
    //    details.OrderSubTotalInclTax = subTotalWithoutDiscountBase;
    //    details.OrderSubTotalDiscountInclTax = orderSubTotalDiscountAmount;

    //    //discount history
    //    foreach (var disc in orderSubTotalAppliedDiscounts)
    //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
    //            details.AppliedDiscounts.Add(disc);

    //    //sub total (excl tax)
    //    (orderSubTotalDiscountAmount, _, subTotalWithoutDiscountBase, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(details.Cart, false);
    //    details.OrderSubTotalExclTax = subTotalWithoutDiscountBase;
    //    details.OrderSubTotalDiscountExclTax = orderSubTotalDiscountAmount;

    //    //shipping info
    //    if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(details.Cart))
    //    {
    //        var pickupPoint = await _genericAttributeService.GetAttributeAsync<PickupPoint>(details.Customer,
    //            NopCustomerDefaults.SelectedPickupPointAttribute, processPaymentRequest.StoreId);
    //        if (_shippingSettings.AllowPickupInStore && pickupPoint != null)
    //        {
    //            var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(pickupPoint.CountryCode);
    //            var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(pickupPoint.StateAbbreviation, country?.Id);

    //            details.PickupInStore = true;
    //            details.PickupAddress = new Address
    //            {
    //                Address1 = pickupPoint.Address,
    //                City = pickupPoint.City,
    //                County = pickupPoint.County,
    //                CountryId = country?.Id,
    //                StateProvinceId = state?.Id,
    //                ZipPostalCode = pickupPoint.ZipPostalCode,
    //                CreatedOnUtc = DateTime.UtcNow
    //            };
    //        }
    //        else
    //        {
    //            if (details.Customer.ShippingAddressId == null)
    //                throw new NopException("Shipping address is not provided");

    //            var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(details.Customer);

    //            if (!CommonHelper.IsValidEmail(shippingAddress?.Email))
    //                throw new NopException("Email is not valid");

    //            //clone shipping address
    //            details.ShippingAddress = _addressService.CloneAddress(shippingAddress);

    //            if (await _countryService.GetCountryByAddressAsync(details.ShippingAddress) is Country shippingCountry && !shippingCountry.AllowsShipping)
    //                throw new NopException($"Country '{shippingCountry.Name}' is not allowed for shipping");
    //        }

    //        var shippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(details.Customer,
    //            NopCustomerDefaults.SelectedShippingOptionAttribute, processPaymentRequest.StoreId);
    //        if (shippingOption != null)
    //        {
    //            details.ShippingMethodName = shippingOption.Name;
    //            details.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
    //        }

    //        details.ShippingStatus = ShippingStatus.NotYetShipped;
    //    }
    //    else
    //        details.ShippingStatus = ShippingStatus.ShippingNotRequired;

    //    //shipping total
    //    var (orderShippingTotalInclTax, _, shippingTotalDiscounts) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, true);
    //    var (orderShippingTotalExclTax, _, _) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(details.Cart, false);
    //    if (!orderShippingTotalInclTax.HasValue || !orderShippingTotalExclTax.HasValue)
    //        throw new NopException("Shipping total couldn't be calculated");

    //    details.OrderShippingTotalInclTax = orderShippingTotalInclTax.Value;
    //    details.OrderShippingTotalExclTax = orderShippingTotalExclTax.Value;

    //    foreach (var disc in shippingTotalDiscounts)
    //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
    //            details.AppliedDiscounts.Add(disc);

    //    //payment total
    //    var paymentAdditionalFee = await _paymentService.GetAdditionalHandlingFeeAsync(details.Cart, processPaymentRequest.PaymentMethodSystemName);
    //    details.PaymentAdditionalFeeInclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, true, details.Customer)).price;
    //    details.PaymentAdditionalFeeExclTax = (await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentAdditionalFee, false, details.Customer)).price;

    //    //tax amount
    //    SortedDictionary<decimal, decimal> taxRatesDictionary;
    //    (details.OrderTaxTotal, taxRatesDictionary) = await _orderTotalCalculationService.GetTaxTotalAsync(details.Cart);

    //    //VAT number
    //    var customerVatStatus = (VatNumberStatus)await _genericAttributeService.GetAttributeAsync<int>(details.Customer, NopCustomerDefaults.VatNumberStatusIdAttribute);
    //    if (_taxSettings.EuVatEnabled && customerVatStatus == VatNumberStatus.Valid)
    //        details.VatNumber = await _genericAttributeService.GetAttributeAsync<string>(details.Customer, NopCustomerDefaults.VatNumberAttribute);

    //    //tax rates
    //    details.TaxRates = taxRatesDictionary.Aggregate(string.Empty, (current, next) =>
    //        $"{current}{next.Key.ToString(CultureInfo.InvariantCulture)}:{next.Value.ToString(CultureInfo.InvariantCulture)};   ");

    //    //order total (and applied discounts, gift cards, reward points)
    //    var (orderTotal, orderDiscountAmount, orderAppliedDiscounts, appliedGiftCards, redeemedRewardPoints, redeemedRewardPointsAmount) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(details.Cart);
    //    if (!orderTotal.HasValue)
    //        throw new NopException("Order total couldn't be calculated");

    //    details.OrderDiscountAmount = orderDiscountAmount;
    //    details.RedeemedRewardPoints = redeemedRewardPoints;
    //    details.RedeemedRewardPointsAmount = redeemedRewardPointsAmount;
    //    details.AppliedGiftCards = appliedGiftCards;
    //    details.OrderTotal = orderTotal.Value;

    //    //discount history
    //    foreach (var disc in orderAppliedDiscounts)
    //        if (!_discountService.ContainsDiscount(details.AppliedDiscounts, disc))
    //            details.AppliedDiscounts.Add(disc);

    //    processPaymentRequest.OrderTotal = details.OrderTotal;

    //    //recurring or standard shopping cart?
    //    details.IsRecurringShoppingCart = await _shoppingCartService.ShoppingCartIsRecurringAsync(details.Cart);
    //    if (!details.IsRecurringShoppingCart)
    //        return details;

    //    var (recurringCyclesError, recurringCycleLength, recurringCyclePeriod, recurringTotalCycles) = await _shoppingCartService.GetRecurringCycleInfoAsync(details.Cart);

    //    if (!string.IsNullOrEmpty(recurringCyclesError))
    //        throw new NopException(recurringCyclesError);

    //    processPaymentRequest.RecurringCycleLength = recurringCycleLength;
    //    processPaymentRequest.RecurringCyclePeriod = recurringCyclePeriod;
    //    processPaymentRequest.RecurringTotalCycles = recurringTotalCycles;

    //    return details;
    //}

    #endregion

    #region Nested classes

    /// <summary>
    /// PlaceOrder container
    /// </summary>
    protected class PlaceOrderContainer
    {
        public PlaceOrderContainer()
        {
            Cart = new List<ShoppingCartItem>();
            AppliedDiscounts = new List<Discount>();
            AppliedGiftCards = new List<AppliedGiftCard>();
        }

        /// <summary>
        /// Customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Customer language
        /// </summary>
        public Language CustomerLanguage { get; set; }

        /// <summary>
        /// Affiliate identifier
        /// </summary>
        public int AffiliateId { get; set; }

        /// <summary>
        /// TAx display type
        /// </summary>
        public TaxDisplayType CustomerTaxDisplayType { get; set; }

        /// <summary>
        /// Selected currency
        /// </summary>
        public string CustomerCurrencyCode { get; set; }

        /// <summary>
        /// Customer currency rate
        /// </summary>
        public decimal CustomerCurrencyRate { get; set; }

        /// <summary>
        /// Billing address
        /// </summary>
        public Address BillingAddress { get; set; }

        /// <summary>
        /// Shipping address
        /// </summary>
        public Address ShippingAddress { get; set; }

        /// <summary>
        /// Shipping status
        /// </summary>
        public ShippingStatus ShippingStatus { get; set; }

        /// <summary>
        /// Selected shipping method
        /// </summary>
        public string ShippingMethodName { get; set; }

        /// <summary>
        /// Shipping rate computation method system name
        /// </summary>
        public string ShippingRateComputationMethodSystemName { get; set; }

        /// <summary>
        /// Is pickup in store selected?
        /// </summary>
        public bool PickupInStore { get; set; }

        /// <summary>
        /// Selected pickup address
        /// </summary>
        public Address PickupAddress { get; set; }

        /// <summary>
        /// Is recurring shopping cart
        /// </summary>
        public bool IsRecurringShoppingCart { get; set; }

        /// <summary>
        /// Initial order (used with recurring payments)
        /// </summary>
        public Order InitialOrder { get; set; }

        /// <summary>
        /// Checkout attributes
        /// </summary>
        public string CheckoutAttributeDescription { get; set; }

        /// <summary>
        /// Shopping cart
        /// </summary>
        public string CheckoutAttributesXml { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<ShoppingCartItem> Cart { get; set; }

        /// <summary>
        /// Applied discounts
        /// </summary>
        public List<Discount> AppliedDiscounts { get; set; }

        /// <summary>
        /// Applied gift cards
        /// </summary>
        public List<AppliedGiftCard> AppliedGiftCards { get; set; }

        /// <summary>
        /// Order subtotal (incl tax)
        /// </summary>
        public decimal OrderSubTotalInclTax { get; set; }

        /// <summary>
        /// Order subtotal (excl tax)
        /// </summary>
        public decimal OrderSubTotalExclTax { get; set; }

        /// <summary>
        /// Subtotal discount (incl tax)
        /// </summary>
        public decimal OrderSubTotalDiscountInclTax { get; set; }

        /// <summary>
        /// Subtotal discount (excl tax)
        /// </summary>
        public decimal OrderSubTotalDiscountExclTax { get; set; }

        /// <summary>
        /// Shipping (incl tax)
        /// </summary>
        public decimal OrderShippingTotalInclTax { get; set; }

        /// <summary>
        /// Shipping (excl tax)
        /// </summary>
        public decimal OrderShippingTotalExclTax { get; set; }

        /// <summary>
        /// Payment additional fee (incl tax)
        /// </summary>
        public decimal PaymentAdditionalFeeInclTax { get; set; }

        /// <summary>
        /// Payment additional fee (excl tax)
        /// </summary>
        public decimal PaymentAdditionalFeeExclTax { get; set; }

        /// <summary>
        /// Tax
        /// </summary>
        public decimal OrderTaxTotal { get; set; }

        /// <summary>
        /// VAT number
        /// </summary>
        public string VatNumber { get; set; }

        /// <summary>
        /// Tax rates
        /// </summary>
        public string TaxRates { get; set; }

        /// <summary>
        /// Order total discount amount
        /// </summary>
        public decimal OrderDiscountAmount { get; set; }

        /// <summary>
        /// Redeemed reward points
        /// </summary>
        public int RedeemedRewardPoints { get; set; }

        /// <summary>
        /// Redeemed reward points amount
        /// </summary>
        public decimal RedeemedRewardPointsAmount { get; set; }

        /// <summary>
        /// Order total
        /// </summary>
        public decimal OrderTotal { get; set; }
    }

    #endregion
}
