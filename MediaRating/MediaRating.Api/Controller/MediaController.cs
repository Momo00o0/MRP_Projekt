using System;
using System.Collections.Generic;
using System.Linq;
using MediaRating.Infrastructure;
using MediaRating.Model;
using MediaRating.DTOs;

namespace MediaRating.Api.Controller
{
    public class MediaController
    {
      private readonly IMediaRatingContext _db;
public MediaController(IMediaRatingContext db) { _db = db; }

        // GET /api/media
        public (List<MediaEntry>? items, int status, string? error) GetAll()
        {
            var items = _db.Media_GetAll();
            return (items, 200, null);
        }

        // GET /api/media/{guid}
        public (MediaEntry? item, int status, string? error) GetByGuid(Guid guid)
        {
            if (guid == Guid.Empty) return (null, 400, "Guid required");

            var item = _db.Media_GetByGuid(guid);
            return item is null ? (null, 404, "Media not found") : (item, 200, null);
        }

        // POST /api/media  
        public (MediaEntry? item, int status, string? error) CreateMedia(MediaEntryDto dto, User requester)
        {
            if (requester is null || requester.Id <= 0) return (null, 401, "Unauthorized");
            if (dto is null) return (null, 400, "Body required");
            if (string.IsNullOrWhiteSpace(dto.Title)) return (null, 400, "Title required");

           
            if (dto.UserGuid != Guid.Empty && dto.UserGuid != requester.Guid)
                return (null, 403, "Forbidden");

            var entity = _db.Media_Create(dto, requester);
            return (entity, 201, null);
        }

        // UPDATE /api/media/{guid}
        public (MediaEntry? item, int status, string? error) UpdateMedia(Guid mediaGuid, MediaUpdateDto dto, User requester)
        {
            if (mediaGuid == Guid.Empty) return (null, 400, "Guid required");
            if (requester is null) return (null, 401, "Unauthorized");

            var existing = _db.Media_GetByGuid(mediaGuid);
            if (existing is null) return (null, 404, "Media not found");

            if (existing.Creator.Guid != requester.Guid)
                return (null, 403, "Forbidden your are not the owner");

            var updated = _db.Media_Update(mediaGuid, dto, requester.Guid);
            if (updated is null) return (null, 500, "Update failed");

            return (updated, 200, null);
        }


        // DELETE /api/media/{guid}
        public (MediaEntry? item, int status, string? error) DeleteMedia(Guid mediaGuid, Guid requesterGuid)
        {
            if (mediaGuid == Guid.Empty) return (null, 400, "Guid required");
            if (requesterGuid == Guid.Empty) return (null, 401, "Unauthorized");

          
            var existing = _db.Media_GetByGuid(mediaGuid);
            if (existing is null) return (null, 404, "Media not found");

            if (existing.Creator.Guid != requesterGuid)
                return (null, 403, "Forbidden, you are not the owner");

            
            var deleted = _db.Media_Delete(mediaGuid, requesterGuid);
            if (deleted is null)
                return (null, 500, "Delete failed");

            return (deleted, 200, null);
        }

        public (object? data, int status, string? error) GetAverageRating(Guid mediaGuid)
        {
            if (mediaGuid == Guid.Empty) return (null, 400, "Guid required");

            var stats = _db.Get_MediaAvg(mediaGuid);
            if (stats is null) return (null, 404, "Media not found");

            return (new
            {
                mediaGuid,
                avgRating = stats.Value.Avg,
                ratingCount = stats.Value.Count
            }, 200, null);
        }

    }
}
