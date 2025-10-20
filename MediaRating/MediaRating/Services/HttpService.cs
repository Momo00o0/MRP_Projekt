using MediaRating.Cmd;
using MediaRating.Controller;
using MediaRating.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace MediaRating.Services;

public class HttpService
{
    readonly HttpListener _l = new();
    readonly DbContext _db;
    readonly UserController _users;
    readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    readonly MediaController _media;
    public HttpService(string url, DbContext db)
    {
        _db = db; 
        _users = new UserController(db);
        _media = new MediaController(db);
        _l.Prefixes.Add(url.EndsWith("/") ? url : url + "/");
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _l.Start();
        Console.WriteLine("Server " + string.Join(", ", _l.Prefixes));
        while (!ct.IsCancellationRequested) _ = Handle(await _l.GetContextAsync());
    }

    async Task Handle(HttpListenerContext c)
    {
        var req = c.Request; var res = c.Response;
        try
        {
            if (req.HttpMethod == "OPTIONS") { Cors(res); res.StatusCode = 204; Close(); return; }

            var path = req.Url!.AbsolutePath.ToLowerInvariant();
            var method = req.HttpMethod.ToUpperInvariant();
            Console.WriteLine($"{method} {path}");

            switch (method)
            {
                case "GET" when path == "/api/users":
                    {
                        var (items, code, err) = _users.GetAllUsers();
                        await Send(code, err is null ? items.Select(u => new { u.Guid, u.Username, u.Id }) : Error(err));
                        break;
                    }
                case "POST" when path == "/api/users/register":
                    {
                        var dto = await Body<UserDto>();
                        var (u, code, err) = _users.AddUser(dto!);
                        await Send(code, err is null ? new { u!.Id, u.Username, u.Guid } : Error(err));
                        break;
                    }
                case "POST" when path == "/api/users/login":
                    {
                        var dto = await Body<UserDto>();
                        var (login, code, err) = _users.Login(dto!);
                        await Send(code, err is null ? login! : Error(err));
                        break;
                    }
                case "PUT" when path.StartsWith("/api/users/put"):
                    {
                        var (ok, tokenUser) = CheckToken(req);
                        if (!ok) { await Send(401, Error("Unauthorized - Valid token required")); break; }

                        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var guid))
                        { await Send(400, Error("Invalid user id")); break; }

                        var target = _db.Users.FirstOrDefault(u => u.Guid == guid);
                        if (target is null) { await Send(404, Error("User Not Found")); break; }
                        if (!string.Equals(target.Username, tokenUser, StringComparison.OrdinalIgnoreCase))
                        { await Send(403, Error("Forbidden")); break; }

                        var cmd = await Body<UpdateUserCmd>();
                        var (u, code, err) = _users.UpdateUser(guid, cmd!);
                        await Send(code, err is null ? new { u!.Id, u.Username, u.Guid } : Error(err));
                        break;
                    }
                // Media
                case "GET" when path == "/api/media/":
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
                                Creator = new {m.Creator.Guid, m.Creator.Username }
                            })
                            : Error(err);
                        await Send(code, payload);
                        break;
                    }

                // GET /api/media/{id}
                case "GET" when path.StartsWith("/api/media/search"):
                    {
                        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var id))
                        { await Send(400, Error("Invalid media id")); break; }

                        var (item, code, err) = _media.GetById(id);
                        if (err != null) { await Send(code, Error(err)); break; }

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
                        await Send(code, dto);
                        break;
                    }

                
                case "POST" when path == "/api/media/add":
                    {
                        var (ok, tokenUser) = CheckToken(req);
                        if (!ok) { await Send(401, Error("Unauthorized - Valid token required")); break; }

                        var dto = await Body<MediaEntryDto>();
                        if (dto is null) { await Send(400, Error("Body required")); break; }
                        if (string.IsNullOrWhiteSpace(dto.Title)) { await Send(400, Error("Title required")); break; }

                        
                        var creator = _db.Users.FirstOrDefault(u => u.Guid == dto.UserGuid);
                        if (creator is null) { await Send(404, Error("Creator (UserGuid) not found")); break; }
                        if (!string.Equals(creator.Username, tokenUser, StringComparison.OrdinalIgnoreCase))
                        { await Send(403, Error("Forbidden")); break; }

                        var (entity, code, err) = _media.Create(dto, creator);
                        if (err != null) { await Send(code, Error(err)); break; }

                        await Send(code, new
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
                    await Send(404, Error("Invalid Path.")); break;
            }

           
            async Task<T?> Body<T>()
            {
                using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
                var s = await sr.ReadToEndAsync();
                return string.IsNullOrWhiteSpace(s) ? default : JsonSerializer.Deserialize<T>(s, _json);
            }

            (bool ok, string? user) CheckToken(HttpListenerRequest r)
            {
                var auth = r.Headers["Authorization"];
                if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return (false, null);
                var parts = auth[7..].Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
                return (parts.Length >= 4 && parts[1].Equals("mrp", StringComparison.OrdinalIgnoreCase)) ? (true, parts[0]) : (false, null);
            }

            object Error(string msg) => new { error = msg };

            async Task Send(int status, object payload)
            {
                Cors(res);
                var json = JsonSerializer.Serialize(payload, _json);
                var bytes = Encoding.UTF8.GetBytes(json);
                res.StatusCode = status; res.ContentType = "application/json; charset=utf-8"; res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                Close();
            }

            void Cors(HttpListenerResponse r)
            {
                r.Headers["Access-Control-Allow-Origin"] = "*";
                r.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
                r.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            }

            void Close()
            {
                try { res.OutputStream.Close(); } catch { }
                try { res.Close(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR " + ex.Message);
            try
            {
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = "Internal Server Error" }));
                res.StatusCode = 500; res.ContentType = "application/json"; res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            finally { try { res.OutputStream.Close(); } catch { } try { res.Close(); } catch { } }
        }
    }
}
