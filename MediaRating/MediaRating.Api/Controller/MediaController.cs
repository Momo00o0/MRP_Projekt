
using System;
using System.Collections.Generic;
using System.Linq;
using MediaRating.Infrastructure; // DbContext
using MediaRating.Model;         // User, MediaEntry, Movie, Series, Game
using MediaRating.DTOs;


namespace MediaRating.Api.Controller
{
    public class MediaController
    {
        private readonly MediaRatingContext _db;
        public MediaController(MediaRatingContext db) { _db = db; }


        // GET /api/media
        public (List<MediaEntry> items, int status, string? error) GetAll()
        {
            var items = _db.Media_GetAll();              // List<MediaEntry>
            return (items, 200, null);                   // Tupel für HttpService
        }

        // GET /api/media/{id}
        public (MediaEntry? item, int status, string? error) GetById(Guid id)
        {
            var e = _db.Media_GetByGuid(id);             // MediaEntry? oder null
            return e is null ? (null, 404, "Not found")
                             : (e, 200, null);
        }

        // POST /api/media/add
        public (MediaEntry? item, int status, string? error) Create(MediaEntryDto dto, User creator)
        {
            if (dto is null) return (null, 400, "Body required");
            if (string.IsNullOrWhiteSpace(dto.Title)) return (null, 400, "Title required");
            if (creator is null || creator.Id <= 0) return (null, 404, "Creator not found");

            var entity = _db.Media_Create(dto, creator); // erzeugt + INSERT
            return (entity, 201, null);
        }
    }
}
    

