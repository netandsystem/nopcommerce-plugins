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
    private readonly ShippingSettings _shippingSettings;
    private readonly ILocalizationService _localizationService;

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
        ILocalizationService localizationService,
        IRepository<Customer> customerRepository
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
        _localizationService = localizationService;
        _customerRepository = customerRepository;
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
        int? sellerId = null
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
                sellerId: sellerId
            );

        var ordersItemQuery = from order in ordersQuery
                              join address in _addressRepository.Table
                              on order.BillingAddressId equals address.Id
                              join orderItem in _orderItemRepository.Table
                              on order.Id equals orderItem.OrderId
                              into orderItemsGroup
                              select order.ToDto(orderItemsGroup.ToList(), address, _paymentService.DeserializeCustomValues(order));

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


    public async Task<PlaceOrderResult> PlaceOrderAsync(OrderPost newOrder, Customer customer, int storeId, IList<ShoppingCartItem> cart)
    {
        newOrder.CustomValuesXml ??= new();

        if (newOrder.PaymentData is not null)
        {
            newOrder.CustomValuesXml.Add("Número de referencia", newOrder.PaymentData.ReferenceNumber);
            newOrder.CustomValuesXml.Add("Monto en Bs", newOrder.PaymentData.AmountInBs);
        }

        bool pickupInStore = newOrder.PickUpInStore ?? false;

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

        var processPaymentRequest = new ProcessPaymentRequest
        {
            StoreId = storeId,
            CustomerId = customer.Id,
            PaymentMethodSystemName = newOrder.PaymentMethodSystemName,
            OrderGuid = Guid.NewGuid(),
            OrderGuidGeneratedOnUtc = DateTime.UtcNow,
            CustomValues = newOrder.CustomValuesXml
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
        int? sellerId = null
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
            query = from order in query
                    join customer in _customerRepository.Table
                        on order.CustomerId equals customer.Id
                    where customer.SellerId == sellerId
                    select order;
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

    #endregion
}
