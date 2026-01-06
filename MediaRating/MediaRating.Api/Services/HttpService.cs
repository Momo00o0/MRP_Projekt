using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MediaRating.Api.Controller;
using MediaRating.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using MediaRating.Api.Cmd;

namespace MediaRating.Api.Services;

/// <summary>
/// Transport-Schicht: Routing, Auth, JSON parsen, HTTP Antworten senden.
/// Keine Business-Logik (Owner-Regeln etc.) – das macht der Controller.
/// </summary>
public class HttpService
{
    private readonly MediaRatingContext _db;
    private readonly UserController _users;
    private readonly MediaController _media;
    private readonly RatingController _ratings;
    private readonly JsonSerializerOptions _json;

    public HttpService(MediaRatingContext db)
    {
        _db = db;
        _users = new UserController(db);
        _media = new MediaController(db);
        _ratings = new RatingController(db);

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        try
        {
            // CORS Preflight (optional)
            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                AddCorsHeaders(res);
                res.StatusCode = 204;
                res.Close();
                return;
            }

            AddCorsHeaders(res);

            // Pfad normalisieren
            var path = req.Url!.AbsolutePath;
            if (path.Length > 1) path = path.TrimEnd('/');
            var method = req.HttpMethod.ToUpperInvariant();

            switch (method)
            {
                // ---------- USERS ----------
                case "GET" when path.Equals("/api/users", StringComparison.OrdinalIgnoreCase):
                    {
                        try
                        {
                            var items = _users.GetAllUsers(); // liefert nur Users
                            await Send(res, 200, items.Select(u => new { u.Id, u.Guid, u.Username }));
                        }
                        catch
                        {
                            await Send(res, 500, Error("Internal server error"));
                        }
                        break;
                    }

                case "POST" when path.Equals("/api/users/register", StringComparison.OrdinalIgnoreCase):
                    {
                        var dto = await Body<UserDto>(req);
                        var (u, code, err) = _users.AddUser(dto!);
                        await Send(res, code, err is null
                            ? new { u!.Id, u.Guid, u.Username }
                            : Error(err));
                        break;
                    }

                case "POST" when path.Equals("/api/users/login", StringComparison.OrdinalIgnoreCase):
                    {
                        var dto = await Body<UserDto>(req);
                        var (login, code, err) = _users.Login(dto!);
                        await Send(res, code, err is null ? login! : Error(err));
                        break;
                    }

                // ---------- MEDIA ----------
              

                case "POST" when path.Equals("/api/media", StringComparison.OrdinalIgnoreCase):
                    {
                        var (ok, user) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var dto = await Body<MediaEntryDto>(req);
                        var (entity, code, err) = _media.CreateMedia(dto!, user!);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        await Send(res, code, new
                        {
                            entity!.Guid,
                            Kind = entity switch { Movie => "Movie", Series => "Series", Game => "Game", _ => "Media" },
                            entity.Title,
                            entity.Description,
                            entity.AgeRestriction,
                            entity.ReleaseYear,
                            Creator = new { entity.Creator.Guid, entity.Creator.Username }
                        });
                        break;
                    }
                case "PUT" when path.StartsWith("/api/media/", StringComparison.OrdinalIgnoreCase):
                    {
                        var (ok, user) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var guidStr = path.Substring("/api/media/".Length).TrimEnd('/');
                        if (!Guid.TryParse(guidStr, out var mediaGuid))
                        { await Send(res, 400, Error("Invalid media guid")); break; }

                        var dto = await Body<MediaUpdateDto>(req);
                        if (dto is null) { await Send(res, 400, Error("Body required")); break; }
                        if (string.IsNullOrWhiteSpace(dto.Title)) { await Send(res, 400, Error("Title required")); break; }
                        if (dto.Description is null) { await Send(res, 400, Error("Description required")); break; } // zur Sicherheit

                        var (entity, code, err) = _media.UpdateMedia(mediaGuid, dto, user!);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        await Send(res, code, new
                        {
                            entity!.Guid,
                            Kind = entity switch { Movie => "Movie", Series => "Series", Game => "Game", _ => "Media" },
                            entity.Title,
                            entity.Description,
                            entity.AgeRestriction,
                            entity.ReleaseYear,
                            Creator = new { entity.Creator.Guid, entity.Creator.Username }
                        });
                        break;
                    }

                case "DELETE" when path.StartsWith("/api/media/", StringComparison.OrdinalIgnoreCase):
                    {
                        var (ok, user) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var guidStr = path.Substring("/api/media/".Length);
                        if (!Guid.TryParse(guidStr, out var mediaGuid))
                        { await Send(res, 400, Error("Invalid media guid")); break; }

                        var (deleted, code, err) = _media.DeleteMedia(mediaGuid, user!.Guid);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        // Üblich bei DELETE: 204 No Content
                        await Send(res, 204, null);
                        break;
                    }

                case "GET" when path.StartsWith("/api/media/avg/", StringComparison.OrdinalIgnoreCase):
                    {
                        var guidStr = path.Substring("/api/media/avg/".Length).TrimEnd('/');
                        if (!Guid.TryParse(guidStr, out var mediaGuid))
                        { await Send(res, 400, Error("Invalid media guid")); break; }

                        var (data, code, err) = _media.GetAverageRating(mediaGuid);
                        await Send(res, code, err is null ? data! : Error(err));
                        break;
                    }
                case "GET" when path.StartsWith("/api/media/", StringComparison.OrdinalIgnoreCase):
                    {
                        var guidStr = path.Substring("/api/media/".Length);
                        if (!Guid.TryParse(guidStr, out var mediaGuid))
                        { await Send(res, 400, Error("Invalid media guid")); break; }

                        var (item, code, err) = _media.GetByGuid(mediaGuid);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        await Send(res, 200, new
                        {
                            item!.Guid,
                            Kind = item switch { Movie => "Movie", Series => "Series", Game => "Game", _ => "Media" },
                            item.Title,
                            item.Description,
                            item.AgeRestriction,
                            item.ReleaseYear,
                            Creator = new { item.Creator.Guid, item.Creator.Username }
                        });
                        break;
                    }

                case "GET" when path.Equals("/api/media", StringComparison.OrdinalIgnoreCase):
                    {
                        var (items, code, err) = _media.GetAll();
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        var payload = items!.Select(m => new
                        {
                            m.Guid,
                            Kind = m switch { Movie => "Movie", Series => "Series", Game => "Game", _ => "Media" },
                            m.Title,
                            m.Description,
                            m.AgeRestriction,
                            m.ReleaseYear,
                            Creator = new { m.Creator.Guid, m.Creator.Username }
                        });

                        await Send(res, 200, payload);
                        break;
                    }

                // ---------- RATINGS ----------
                case "POST" when path.Equals("/api/ratings", StringComparison.OrdinalIgnoreCase):
                    {
                        var dto = await Body<RatingCreateDto>(req);
                        var (msg, code, err) = _ratings.Create(dto!);
                        await Send(res, code, err is null ? new { message = msg } : Error(err));
                        break;
                    }

                case "GET" when path.StartsWith("/api/ratings/media", StringComparison.OrdinalIgnoreCase)
                             && !path.EndsWith("/avg", StringComparison.OrdinalIgnoreCase):
                    {
                        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 4 || !Guid.TryParse(parts[3], out var mg))
                        { await Send(res, 400, Error("Invalid media guid")); break; }

                        var (items, code, err) = _ratings.GetForMedia(mg);
                        await Send(res, code, err is null ? items!.Select(items => new
                        {
                            items.Stars,
                            items.Comment,
                            items.Timestamp,
                            items.Confirmed,
                            items.Guid,
                            Creator = new { items.Creator.Username },   
                            Media = new { items.Media.Title }           
                        }) : Error(err));
                        break;
                    }

                case "PUT" when path.StartsWith("/api/ratings/", StringComparison.OrdinalIgnoreCase):
                    {
                        var (ok, user) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized")); break; }

                        var guidStr = path.Substring("/api/ratings/".Length).TrimEnd('/');
                        if (!Guid.TryParse(guidStr, out var ratingGuid))
                        { await Send(res, 400, Error("Invalid rating guid")); break; }

                        var dto = await Body<RatingUpdateDto>(req);
                        if (dto is null) { await Send(res, 400, Error("Body required")); break; }

                        var (item, code, err) = _ratings.UpdateRating(ratingGuid, dto, user!);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        // Response ohne Passwort (nur Username)
                        await Send(res, 200, new
                        {
                            item!.Guid,
                            item.Stars,
                            item.Comment,
                            item.Timestamp,
                            item.Confirmed,
                            Creator = new { item.Creator.Guid, item.Creator.Username },
                            Media = new { item.Media.Guid, item.Media.Title }
                        });

                        break;
                    }

                case "DELETE" when path.StartsWith("/api/ratings/", StringComparison.OrdinalIgnoreCase):
                    {
                        var (ok, user) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var guidStr = path.Substring("/api/ratings/".Length).TrimEnd('/');
                        if (!Guid.TryParse(guidStr, out var ratingGuid))
                        { await Send(res, 400, Error("Invalid rating guid")); break; }

                        var (success, code, err) = _ratings.DeleteRating(ratingGuid, user!);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        await Send(res, 204, null);
                        break;
                    }

                default:
                    await Send(res, 404, Error("Invalid Path."));
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();

            await Send(res, 500, Error("Internal Server Error"));
        }
    }

    // -------------------- Helpers --------------------

    private (bool ok, User? user) CheckToken(HttpListenerRequest req)
    {
        var auth = req.Headers["Authorization"];
        if (string.IsNullOrWhiteSpace(auth)) return (false, null);

        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return (false, null);

        var token = auth.Substring("Bearer ".Length).Trim();
        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);

        // Erwartet: mrpx.<UserGuidImFormatN>.<...>.<...>
        if (parts.Length != 4) return (false, null);
        if (!parts[0].Equals("mrpx", StringComparison.OrdinalIgnoreCase)) return (false, null);

        if (!Guid.TryParseExact(parts[1], "N", out var userGuid)) return (false, null);

        var user = _db.Users_FindByGuid(userGuid);
        return user is null ? (false, null) : (true, user);
    }

    private async Task<T?> Body<T>(HttpListenerRequest req)
    {
        using var sr = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
        var raw = await sr.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(raw)) return default;
        return JsonSerializer.Deserialize<T>(raw, _json);
    }

    private async Task Send(HttpListenerResponse res, int status, object? payload)
    {
        res.StatusCode = status;

        if (payload is null)
        {
            res.Close();
            return;
        }

        var json = JsonSerializer.Serialize(payload, _json);
        var data = Encoding.UTF8.GetBytes(json);

        res.ContentType = "application/json; charset=utf-8";
        res.ContentEncoding = Encoding.UTF8;
        res.ContentLength64 = data.Length;

        await res.OutputStream.WriteAsync(data, 0, data.Length);
        res.Close();
    }

    private object Error(string message) => new { error = message };

    private void AddCorsHeaders(HttpListenerResponse res)
    {
        res.Headers["Access-Control-Allow-Origin"] = "*";
        res.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
        res.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
    }
}
