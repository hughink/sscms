﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SSCMS.Enums;
using SSCMS.Utils;

namespace SSCMS.Web.Controllers.Home.Common.Form
{
    public partial class LayerVideoUploadController
    {
        [HttpPost, Route(Route)]
        public async Task<ActionResult<List<SubmitResult>>> Submit([FromBody] SubmitRequest request)
        {
            var siteIds = await _authManager.GetSiteIdsAsync();
            if (!ListUtils.Contains(siteIds, request.SiteId)) return Unauthorized();

            var site = await _siteRepository.GetAsync(request.SiteId);
            if (site == null) return this.Error("无法确定内容对应的站点");

            var isAutoStorage = await _storageManager.IsAutoStorageAsync(request.SiteId, SyncType.Videos);

            var result = new List<SubmitResult>();
            foreach (var filePath in request.FilePaths)
            {
                if (string.IsNullOrEmpty(filePath)) continue;

                var virtualUrl = await _pathManager.GetVirtualUrlByPhysicalPathAsync(site, filePath);
                var fileUrl = await _pathManager.ParseSiteUrlAsync(site, virtualUrl, true);
                if (isAutoStorage)
                {
                    var (success, url) = await _storageManager.StorageAsync(request.SiteId, filePath);
                    if (success)
                    {
                        virtualUrl = fileUrl = url;
                    }
                }

                result.Add(new SubmitResult
                {
                    FileUrl = fileUrl,
                    FileVirtualUrl = virtualUrl
                });
            }

            var options = TranslateUtils.JsonDeserialize(site.Get<string>($"Home.{nameof(LayerVideoUploadController)}"), new Options
            {
                IsChangeFileName = true
            });

            options.IsChangeFileName = request.IsChangeFileName;
            site.Set($"Home.{nameof(LayerVideoUploadController)}", TranslateUtils.JsonSerialize(options));

            await _siteRepository.UpdateAsync(site);

            return result;
        }
    }
}
