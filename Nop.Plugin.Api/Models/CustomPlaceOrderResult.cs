using Nop.Core.Domain.Orders;
using Nop.Services.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Models;

public class CustomPlaceOrderResult : PlaceOrderResult
{
    public List<Order> PlacedOrders { get; set; } = new();
}
