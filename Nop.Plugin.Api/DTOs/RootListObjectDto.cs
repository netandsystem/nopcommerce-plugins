using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Api.DTO;
using Nop.Plugin.Api.DTO.Base;

namespace Nop.Plugin.Api.DTOs;

#nullable enable

public class RootListObjectDto<TDto> : ISerializableObject
    where TDto : BaseDto
{
    [JsonProperty("content")]
    public readonly IList<TDto> Content;

    [JsonProperty("hasNextPage")]
    public readonly bool HasNextPage;

    [JsonProperty("count")]
    public readonly int Count;

    [JsonProperty("firstId")]
    public readonly int FirstId;

    [JsonProperty("lastId")]
    public readonly int LastId;

    public RootListObjectDto(IPagedList<TDto> content)
    {
        Content = content;
        HasNextPage = content.HasNextPage;
        Count = content.Count;
        FirstId = content.FirstOrDefault()?.Id ?? 0;
        LastId = content.LastOrDefault()?.Id ?? 0;
    }
 
    public string GetPrimaryPropertyName()
    {
        return "content";
    }

    public Type GetPrimaryPropertyType()
    {
        return typeof(TDto);
    }
}
