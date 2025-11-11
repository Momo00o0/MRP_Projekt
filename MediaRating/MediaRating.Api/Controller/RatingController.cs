using System;
using System.Collections.Generic;
using MediaRating.Infrastructure;
using MediaRating.DTOs;
using MediaRating.Model;

namespace MediaRating.Api.Controller
{
    public class RatingController
    {
        private readonly MediaRatingContext _db;
        public RatingController(MediaRatingContext db) => _db = db;

        // POST /api/ratings  (nur neu, kein Update)
        public (string msg, int status, string? error) Create(RatingCreateDto dto)
        {
            if (dto is null) return ("", 400, "Body required");
            if (dto.Stars < 1 || dto.Stars > 5) return ("", 400, "Stars must be 1 to 5");

            try
            {
                if (_db.Ratings_Exists(dto.UserGuid, dto.MediaGuid))
                    return ("", 409, "Rating already exists"); 

                _db.Ratings_Insert(dto.UserGuid, dto.MediaGuid, dto.Stars, dto.Comment);
                return ("created", 201, null);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("User not found"))
            {
                return ("", 404, "User not found");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Media not found"))
            {
                return ("", 404, "Media not found");
            }
            catch
            {
                return ("", 500, "Create rating failed");
            }
        }

        // GET /api/ratings/media/{mediaGuid}
        public (List<Rating> items, int status, string? error) GetForMedia(Guid mediaGuid)
        {
            try
            {
                var items = _db.Ratings_GetForMedia(mediaGuid);
                return (items, 200, null);
            }
            catch
            {
                return (new List<Rating>(), 500, "Read ratings failed");
            }
        }

        // (optional) GET /api/ratings/user/{userGuid}
        public (List<Rating> items, int status, string? error) GetForUser(Guid userGuid)
        {
            try
            {
                var items = _db.Ratings_GetForUser(userGuid);
                return (items, 200, null);
            }
            catch
            {
                return (new List<Rating>(), 500, "Read ratings failed");
            }
        }

       

        // DELETE /api/ratings?user={u}&media={m}
        public (string msg, int status, string? error) Delete(Guid userGuid, Guid mediaGuid)
        {
            try
            {
                var rows = _db.Ratings_Delete(userGuid, mediaGuid);
                return rows > 0 ? ("deleted", 200, null) : ("", 404, "Rating not found");
            }
            catch
            {
                return ("", 500, "Delete failed");
            }
        }
    }
}
