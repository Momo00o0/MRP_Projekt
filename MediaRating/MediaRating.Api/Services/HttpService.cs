using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaRating.Infrastructure;
using MediaRating.Api.Controller;
using MediaRating.Api.DTOs;
using MediaRating.Model;
using MediaRating.Api.Cmd;

namespace MediaRating.Api.Services;

public class HttpService
{
    private readonly DbContext _db;
    private readonly UserController _users;
    private readonly MediaController _media;
    private readonly JsonSerializerOptions _json;

    public HttpService(DbContext db)
    {
        _db = db;
        _users = new UserController(db);
        _media = new MediaController(db);

        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    // wie bei deinem Kollegen: nur ein Handler, den Program.cs aufruft
    public async Task HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        try
        {
            // CORS preflight
            if (req.HttpMethod == "OPTIONS")
            {
                Cors(res);
                res.StatusCode = 204;
                await res.OutputStream.FlushAsync();
                res.Close();
                return;
            }

            var path = req.Url!.AbsolutePath.ToLowerInvariant();
            if (path.Length > 1) path = path.TrimEnd('/'); // trailing slash toleranter
            var method = req.HttpMethod.ToUpperInvariant();

            Console.WriteLine($"{method} {path}");

            switch (method)
            {
                // ---------- USERS ----------
                case "GET" when path == "/api/users":
                    {
                        var (items, code, err) = _users.GetAllUsers();
                        await Send(res, code, err is null
                            ? items.Select(u => new { u.Guid, u.Username, u.Id })
                            : Error(err));
                        break;
                    }

                case "POST" when path == "/api/users/register":
                    {
                        var dto = await Body<UserDto>(req);
                        var (u, code, err) = _users.AddUser(dto!);
                        await Send(res, code, err is null
                            ? new { u!.Id, u.Username, u.Guid }
                            : Error(err));
                        break;
                    }

                case "POST" when path == "/api/users/login":
                    {
                        var dto = await Body<UserDto>(req);
                        var (login, code, err) = _users.Login(dto!);
                        await Send(res, code, err is null ? login! : Error(err));
                        break;
                    }

                case "PUT" when path.StartsWith("/api/users/"):
                    {
                        var (ok, tokenUser) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // ["api","users","{guid}"]
                        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var guid))
                        { await Send(res, 400, Error("Invalid user id")); break; }

                        var target = _db.Users.FirstOrDefault(u => u.Guid == guid);
                        if (target is null) { await Send(res, 404, Error("User Not Found")); break; }
                        if (!string.Equals(target.Username, tokenUser, StringComparison.OrdinalIgnoreCase))
                        { await Send(res, 403, Error("Forbidden")); break; }

                        var cmd = await Body<UpdateUserCmd>(req);
                        var (u, code, err) = _users.UpdateUser(guid, cmd!);
                        await Send(res, code, err is null
                            ? new { u!.Id, u.Username, u.Guid }
                            : Error(err));
                        break;
                    }

                // ---------- MEDIA ----------
                case "GET" when path == "/api/media":
                    {
                        var (items, code, err) = _media.GetAll();
                        var payload = err is null
                            ? items.Select(m => new {
                                m.Guid,
                                Kind = m switch { Movie => "Movie", Series => "Series", Game => "Game", _ => "Media" },
                                m.Title,
                                m.Description,
                                m.AgeRestriction,
                                m.ReleaseYear,
                                Creator = new { m.Creator.Guid, m.Creator.Username }
                            })
                            : Error(err);
                        await Send(res, code, payload);
                        break;
                    }

                // GET /api/media/{id}
                case "GET" when path.StartsWith("/api/media/"):
                    {
                        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var id))
                        { await Send(res, 400, Error("Invalid media id")); break; }

                        var (item, code, err) = _media.GetById(id);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        var dto = new
                        {
                            item!.Guid,
                            Kind = item switch { Movie => "Movie", Series => "Series", Game => "Game", _ => "Media" },
                            item.Title,
                            item.Description,
                            item.AgeRestriction,
                            item.ReleaseYear,
                            Creator = new { item.Creator.Guid, item.Creator.Username }
                        };
                        await Send(res, code, dto);
                        break;
                    }

                // POST /api/media/add
                case "POST" when path == "/api/media/add":
                    {
                        var (ok, tokenUser) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var dto = await Body<MediaEntryDto>(req);
                        if (dto is null) { await Send(res, 400, Error("Body required")); break; }
                        if (string.IsNullOrWhiteSpace(dto.Title)) { await Send(res, 400, Error("Title required")); break; }

                        var creator = _db.Users.FirstOrDefault(u => u.Guid == dto.UserGuid);
                        if (creator is null) { await Send(res, 404, Error("Creator (UserGuid) not found")); break; }
                        if (!string.Equals(creator.Username, tokenUser, StringComparison.OrdinalIgnoreCase))
                        { await Send(res, 403, Error("Forbidden")); break; }

                        var (entity, code, err) = _media.Create(dto, creator);
                        if (err != null) { await Send(res, code, Error(err)); break; }

                        await Send(res, code, new
                        {
                            entity!.Guid,
                            Kind = dto.Kind.ToString(),
                            entity.Title,
                            entity.Description,
                            entity.AgeRestriction,
                            entity.ReleaseYear,
                            Creator = new { entity.Creator.Guid, entity.Creator.Username }
                        });
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
            Console.WriteLine("ERR " + ex.Message);
            Console.ResetColor();
            await Send(ctx.Response, 500, Error("Internal Server Error"));
        }

        // ---- local helpers ----
        async Task<T?> Body<T>(HttpListenerRequest r)
        {
            using var sr = new StreamReader(r.InputStream, r.ContentEncoding);
            var s = await sr.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(s) ? default : JsonSerializer.Deserialize<T>(s, _json);
        }

        (bool ok, string? username) CheckToken(HttpListenerRequest r)
        {
            var auth = r.Headers["Authorization"];
            if (auth is null || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return (false, null);

            var parts = auth[7..].Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || !parts[0].Equals("mrpx", StringComparison.OrdinalIgnoreCase)) return (false, null);

            if (!Guid.TryParseExact(parts[1], "N", out var userGuid)) return (false, null);
            var u = _db.Users.FirstOrDefault(x => x.Guid == userGuid);
            return u is null ? (false, null) : (true, u.Username);
        }


        object Error(string msg) => new { error = msg };

        async Task Send(HttpListenerResponse r, int status, object payload)
        {
            Cors(r);
            var json = JsonSerializer.Serialize(payload, _json);
            var bytes = Encoding.UTF8.GetBytes(json);
            r.StatusCode = status;
            r.ContentType = "application/json; charset=utf-8";
            r.ContentLength64 = bytes.Length;
            await r.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            try { r.OutputStream.Close(); } catch { }
            try { r.Close(); } catch { }
        }

        void Cors(HttpListenerResponse r)
        {
            r.Headers["Access-Control-Allow-Origin"] = "*";
            r.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
            r.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        }
    }
}
