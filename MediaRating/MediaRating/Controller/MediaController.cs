
using MediaRating.Infrastructure;
using MediaRating.Model;
using MediaRating.DTOs;

namespace MediaRating.Controller
{
    public class MediaController
    {
        private readonly DbContext _db;
        public MediaController(DbContext db) { _db = db; }

       
        public (List<MediaEntry> items, int status, string? error) GetAll()
        {
            try { return (_db.MediaEntries.ToList(), 200, null); }
            catch { return (new(), 500, "Failed to get media."); }
        }

        
        public (MediaEntry? item, int status, string? error) GetById(Guid id)
        {
            try
            {
                var e = _db.MediaEntries.FirstOrDefault(x => x.Guid == id);
                return e is null ? (null, 404, "Not found") : (e, 200, null);
            }
            catch { return (null, 500, "Failed to get media."); }
        }

       
        public (MediaEntry? item, int status, string? error) Create(MediaEntryDto dto, User creator)
        {
            try
            {
                if (dto is null) return (null, 400, "Body required");
                if (string.IsNullOrWhiteSpace(dto.Title)) return (null, 400, "Title required");

                MediaEntry entity = dto.Kind switch
                {
                    MediaKind.Movie => new Movie(dto.Title, dto.Description, dto.ReleaseYear, dto.AgeRestriction, creator) { Guid = Guid.NewGuid() },
                    MediaKind.Series => new Series(dto.Title, dto.Description, dto.ReleaseYear, dto.AgeRestriction, creator) { Guid = Guid.NewGuid() },
                    MediaKind.Game => new Game(dto.Title, dto.Description, dto.ReleaseYear, dto.AgeRestriction, creator) { Guid = Guid.NewGuid() },
                    _ => throw new ArgumentOutOfRangeException()
                };

                

                _db.MediaEntries.Add(entity);
                return (entity, 201, null);
            }
            catch { return (null, 500, "Create failed"); }
        }
    }
}
