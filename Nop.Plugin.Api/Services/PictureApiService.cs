using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Api.DTO.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Api.Services;

#nullable enable

public class PictureApiService<T, TDto, TPicture> where T : BaseEntity where TDto : BaseDto where TPicture : BaseEntity
{
    #region Attributes

    private readonly IRepository<Picture> _pictureRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MediaSettings _mediaSettings;
    private readonly IWebHelper _webHelper;

    private IRepository<TPicture>? _repository = null;
    private Func<TPicture, Picture>? _getPictureFromTPicture = null;
    private Func<TPicture, int>? _getItemIdFromTPicture = null;

    #endregion

    #region Ctr
    public PictureApiService(
        IRepository<Picture> pictureRepository,
        IHttpContextAccessor httpContextAccessor,
        MediaSettings mediaSettings,
        IWebHelper webHelper
    )
    {
        _pictureRepository = pictureRepository;
        _httpContextAccessor = httpContextAccessor;
        _mediaSettings = mediaSettings;
        _webHelper = webHelper;
    }

    #endregion

    public async Task<IList<TDto>> JoinProductsAndPicturesAsync(
        IList<T> items,
        IRepository<TPicture> repository,
        Func<T, List<string>, TDto> toDto,
        Func<TPicture, Picture> getPictureFromTPicture,
        Func<TPicture, int>? getItemIdFromTPicture
    )
    {
        _repository = repository;
        _getPictureFromTPicture = getPictureFromTPicture;
        _getItemIdFromTPicture = getItemIdFromTPicture;

        var pictures = await GetItemPicturesAsync(items);

        string imagePathUrl = await GetImagesPathUrlAsync();

        var query = from item in items
                    join picture in pictures
                    on item.Id equals picture.ItemId into productImagesGroup
                    select toDto(item, productImagesGroup.Select(item => GetPictureUrl(item.Picture, imagePathUrl)).ToList());

        return await query.ToListAsync();
    }

    public async Task<TDto> AddPicturesToItemAsync(
        IRepository<TPicture> repository,
        T item,
        Func<T, List<string>, TDto> toDto,
        Func<TPicture, Picture> getPictureFromTPicture,
        Func<TPicture, int>? getItemIdFromTPicture
    )
    {
        _repository = repository;
        _getPictureFromTPicture = getPictureFromTPicture;
        _getItemIdFromTPicture = getItemIdFromTPicture;

        var pictures = await GetItemPicturesAsync(new List<T>() { item });

        string imagePathUrl = await GetImagesPathUrlAsync();

        var itemDto = toDto(item, pictures.Select(item => GetPictureUrl(item.Picture, imagePathUrl)).ToList());

        return itemDto;
    }

    #region Private methods

    private async Task<IList<InternalItemPicture>> GetItemPicturesAsync(IList<T> items)
    {
        var productPicturesQuery = GetItemPicturesQuery();

        var query = from pp in productPicturesQuery
                    join p in items
                    on pp.ItemId equals p.Id
                    select pp;

        return await query.ToListAsync();
    }

    private IQueryable<InternalItemPicture> GetItemPicturesQuery()
    {
        if (_repository == null)
        {
            throw new InvalidOperationException("Attribute _repository is null");
        }

        if (_getPictureFromTPicture == null)
        {
            throw new InvalidOperationException("Attribute _getPictureFromTPicture is null");
        }

        if (_getItemIdFromTPicture == null)
        {
            throw new InvalidOperationException("Attribute _getItemIdFromTPicture is null");
        }

        var query = from pp in _repository.Table
                    join picture in _pictureRepository.Table
                    on _getPictureFromTPicture(pp).Id equals picture.Id
                    select new InternalItemPicture(picture, _getItemIdFromTPicture(pp));

        return query;
    }

    private string GetPictureUrl(Picture picture, string imagesPathUrl)
    {
        var seoFileName = picture.SeoFilename; // = GetPictureSeName(picture.SeoFilename); //just for sure

        var lastPart = GetFileExtensionFromMimeTypeAsync(picture.MimeType);

        string thumbFileName = !string.IsNullOrEmpty(seoFileName)
            ? $"{picture.Id:0000000}_{seoFileName}.{lastPart}"
            : $"{picture.Id:0000000}.{lastPart}";

        return GetThumbUrlAsync(thumbFileName, imagesPathUrl);
    }

    private string GetFileExtensionFromMimeTypeAsync(string mimeType)
    {
        var parts = mimeType.Split('/');
        var lastPart = parts[^1];
        switch (lastPart)
        {
            case "pjpeg":
                lastPart = "jpg";
                break;
            case "x-png":
                lastPart = "png";
                break;
            case "x-icon":
                lastPart = "ico";
                break;
            default:
                break;
        }

        return lastPart;
    }

    private string GetThumbUrlAsync(string thumbFileName, string imagesPathUrl)
    {
        var url = imagesPathUrl + "thumbs/";
        url += thumbFileName;
        return url;
    }

    private Task<string> GetImagesPathUrlAsync()
    {
        var pathBase = _httpContextAccessor?.HttpContext?.Request?.PathBase.Value ?? string.Empty;
        var imagesPathUrl = _mediaSettings.UseAbsoluteImagePath ? null : $"{pathBase}/";
        imagesPathUrl = string.IsNullOrEmpty(imagesPathUrl) ? _webHelper.GetStoreLocation() : imagesPathUrl;
        imagesPathUrl += "images/";

        return Task.FromResult(imagesPathUrl);
    }

    #endregion

    #region Private classes

    private class InternalItemPicture
    {
        public Picture Picture { get; set; }
        public int ItemId { get; set; }

        public InternalItemPicture(Picture picture, int productId)
        {
            Picture = picture;
            ItemId = productId;
        }
    }

    #endregion
}
