using System;
using System.Collections.Generic;
using MediaRating.Infrastructure;
using MediaRating.DTOs;
using MediaRating.Model;

namespace MediaRating.Api.Controller
{
    public class RatingController
    {
        private readonly IMediaRatingContext _db;
        public RatingController(IMediaRatingContext db) { _db = db; }

        // POST /api/ratings  (nur neu, kein Update)
        public (string msg, int status, string? error) Create(RatingCreateDto dto)
        {
            if (dto is null) return ("", 400, "Body required");
            if (dto.Stars < 1 || dto.Stars > 5) return ("", 400, "Stars must be 1 to 5");

            try
            {
                if (_db.Ratings_Exists(dto.UserGuid, dto.MediaGuid))
                    return ("", 409, "You already gave a rating to this MediaEntry"); 

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

        public (Rating? item, int status, string? error) UpdateRating(Guid ratingGuid, RatingUpdateDto dto, User requester)
        {
            if (ratingGuid == Guid.Empty) return (null, 400, "Guid required");
            if (requester is null) return (null, 401, "Unauthorized");

            if (dto.Stars < 1 || dto.Stars > 5)
                return (null, 400, "Stars must be between 1 and 5");


            var updated = _db.Rating_Update(ratingGuid, requester.Guid, dto);
            if (updated is null) return (null, 500, "Update failed");

            return (updated, 200, null);
        }


        public (bool ok, int status, string? error) DeleteRating(Guid ratingGuid, User requester)
        {
            if (ratingGuid == Guid.Empty) return (false, 400, "Guid required");
            if (requester is null) return (false, 401, "Unauthorized");

         

            var ok = _db.Rating_Delete(ratingGuid, requester.Guid);
            if (!ok) return (false, 500, "Delete rating failed");

            return (true, 204, null);
        }



    }
}
