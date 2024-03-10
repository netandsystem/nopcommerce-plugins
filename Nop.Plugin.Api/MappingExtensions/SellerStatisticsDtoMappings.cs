using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Statistics;
using Nop.Plugin.Api.AutoMapper;
using Nop.Plugin.Api.DTO.Customers;
using Nop.Plugin.Api.DTOs.Statistics;

namespace Nop.Plugin.Api.MappingExtensions;

public static class SellerStatisticsDtoMappings
{
    public static SellerStatisticsDto ToDto(this SellerStatistics item)
    {
        return item.MapTo<SellerStatistics, SellerStatisticsDto>();
    }
}
