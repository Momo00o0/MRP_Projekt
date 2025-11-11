using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaRating.Infrastructure;
using MediaRating.Api.Controller;
using MediaRating.DTOs;
using MediaRating.Model;
using MediaRating.Api.Cmd;

/* ============================================================
   SCHRITT: Namensraum festlegen
   EINFACH ERKLÄRT:
   - Alles in diesem Block gehört zur API-Services-Schicht.
   - So bleibt der Code ordentlich einsortiert.
   ============================================================ */
namespace MediaRating.Api.Services;

public class HttpService
{
    /* ============================================================
       SCHRITT: Felder anlegen (Abhängigkeiten & Einstellungen)
       EINFACH ERKLÄRT:
       - _db: Verbindung zur Datenbank, um Daten zu lesen/schreiben.
       - _users: Controller, der alle Benutzer-Aktionen kapselt.
       - _media: Controller, der alle Medien-Aktionen kapselt.
       - _json: Regeln, wie JSON gelesen und geschrieben wird
                (z.B. Groß-/Kleinschreibung egal, hübsch formatiert).
       ============================================================ */
    private readonly MediaRatingContext _db;
    private readonly UserController _users;
    private readonly MediaController _media;
    private readonly RatingController _ratings;
    private readonly JsonSerializerOptions _json;

    public HttpService(MediaRatingContext db)
    {
        _db = db;                               // Datenbank merken
        _users = new UserController(db);        // Benutzer-Controller erzeugen
        _media = new MediaController(db);       // Medien-Controller erzeugen
        _ratings = new RatingController(db);

        _json = new JsonSerializerOptions       // JSON-Verhalten einstellen
        {
            PropertyNameCaseInsensitive = true, // "username" == "UserName" usw.
            WriteIndented = true                // Antworten lesbarer ausgeben
        };
        _json.Converters.Add(new JsonStringEnumConverter()); // Enums als Text (z.B. "Movie")
    }


    public async Task HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;  // einkommender HTTP-Request
        var res = ctx.Response; // Antwortobjekt zum Zurücksenden

        /* ============================================================
           SCHRITT: Request robust verarbeiten (try/catch)
           EINFACH ERKLÄRT:
           - Wir versuchen die Anfrage sauber zu bearbeiten.
           - Falls etwas Unerwartetes passiert, geben wir einen
             "500 Internal Server Error" zurück, statt abzustürzen.
           ============================================================ */
        try
        {
            /* ============================================================
               SCHRITT: CORS-Preflight (OPTIONS)
               EINFACH ERKLÄRT:
               - Browser schicken vor manchen Anfragen zuerst OPTIONS,
                 um zu prüfen, ob sie dürfen (CORS).
               - Wir beantworten das mit Status 204 und den passenden
                 CORS-Headern – ohne weiteren Inhalt.
               ============================================================ */
            if (req.HttpMethod == "OPTIONS")
            {
                Cors(res);
                res.StatusCode = 204;
                await res.OutputStream.FlushAsync();
                res.Close();
                return; // hier ist die Preflight-Anfrage erledigt
            }

            /* ============================================================
               SCHRITT: Pfad & Methode normalisieren
               EINFACH ERKLÄRT:
               - Pfad in Kleinbuchstaben, abschließenden "/" entfernen
                 (damit "/api/users" und "/api/users/" gleich behandelt werden).
               - HTTP-Methode (GET/POST/PUT/...) groß schreiben.
               - Zu Debug-Zwecken in die Konsole loggen.
               ============================================================ */
            var path = req.Url!.AbsolutePath.ToLowerInvariant();
            if (path.Length > 1) path = path.TrimEnd('/'); // trailing slash toleranter
            var method = req.HttpMethod.ToUpperInvariant();

            Console.WriteLine($"{method} {path}");

            /* ============================================================
               SCHRITT: Einfache Routing-Logik
               EINFACH ERKLÄRT:
               - Je nach HTTP-Methode und Pfad wird ein passender
                 Controller-Aufruf ausgeführt.
               - Jeder Fall kümmert sich um:
                   1) Eingabedaten lesen (Body/Pfad/Headers)
                   2) Controller verwenden (Fachlogik)
                   3) Ergebnis in eine saubere JSON-Antwort packen
                 oder einen passenden Fehler senden (400/401/403/404/...).
               ============================================================ */
            switch (method)
            {
                // ---------- USERS ----------
                case "GET" when path == "/api/users":
                    {
                        /* ----------------------------------------------
   SCHRITT: Alle Benutzer holen
   EINFACH ERKLÄRT:
   - Keine Eingabe nötig.
   - Controller fragt die DB nach allen Nutzern.
   - Wir geben nur harmlose Felder zurück
     (Guid, Username, Id).
   ---------------------------------------------- */
                        var items = _users.GetAllUsers(); // nur Liste
                        await Send(res, 200, items.Select(u => new { u.Guid, u.Username, u.Id }));
                        break;
                    }

                case "POST" when path == "/api/users/register":
                    {
                        /* ----------------------------------------------
                           SCHRITT: Benutzer registrieren
                           EINFACH ERKLÄRT:
                           - JSON-Body (UserDto) einlesen (z.B. Username, Passwort).
                           - Controller legt den Benutzer an (inkl. Validierung).
                           - Bei Erfolg: Grunddaten des neuen Nutzers zurück.
                           - Bei Fehler: eine klare Fehlermeldung.
                           ---------------------------------------------- */
                        var dto = await Body<UserDto>(req);
                        var (u, code, err) = _users.AddUser(dto!);
                        await Send(res, code, err is null
                            ? new { u!.Id, u.Username, u.Guid }
                            : Error(err));
                        break;
                    }

                case "POST" when path == "/api/users/login":
                    {
                        /* ----------------------------------------------
                           SCHRITT: Benutzer einloggen
                           EINFACH ERKLÄRT:
                           - Login-Daten (UserDto) aus dem JSON-Body lesen.
                           - Controller prüft Zugangsdaten und erzeugt
                             bei Erfolg ein Login-Ergebnis (z.B. Token).
                           - Antwort enthält Login-Infos oder Fehler.
                           ---------------------------------------------- */
                        var dto = await Body<UserDto>(req);
                        var (login, code, err) = _users.Login(dto!);
                        await Send(res, code, err is null ? login! : Error(err));
                        break;
                    }

              

                // ---------- MEDIA ----------
                case "GET" when path == "/api/media":
                    {
                        /* ----------------------------------------------
                           SCHRITT: Alle Medien auflisten
                           EINFACH ERKLÄRT:
                           - Controller liefert alle Medienobjekte.
                           - Wir formen daraus eine einfache, API-freundliche
                             Antwort (Guid, Titel, Beschreibung, Typ, etc.).
                           - Creator wird schlank als (Guid, Username) zurückgegeben.
                           ---------------------------------------------- */
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
                        /* ----------------------------------------------
                           SCHRITT: Einzelnes Medium laden
                           EINFACH ERKLÄRT:
                           - GUID aus dem Pfad lesen und prüfen.
                           - Controller holt das Medium aus der DB.
                           - Bei Erfolg: Details zum Medium + Creator zurück.
                           - Bei Fehler: passende Fehlermeldung (400/404/...).
                           ---------------------------------------------- */
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
                        /* ----------------------------------------------
                           SCHRITT: Neues Medium anlegen
                           EINFACH ERKLÄRT:
                           - Authentifizierung prüfen (nur eingeloggte Nutzer).
                           - JSON-Body (MediaEntryDto) lesen und Pflichtfelder
                             prüfen (z.B. Titel).
                           - Sicherstellen, dass der "Creator" (UserGuid) existiert
                             und mit dem eingeloggten Nutzer übereinstimmt.
                           - Controller erstellt das Medium.
                           - Erfolgsdaten (inkl. Typ, Creator) zurückgeben.
                           ---------------------------------------------- */
                        var (ok, tokenUser) = CheckToken(req);
                        if (!ok) { await Send(res, 401, Error("Unauthorized - Valid token required")); break; }

                        var dto = await Body<MediaEntryDto>(req);
                        if (dto is null) { await Send(res, 400, Error("Body required")); break; }
                        if (string.IsNullOrWhiteSpace(dto.Title)) { await Send(res, 400, Error("Title required")); break; }

                        var creator = _db.Users_FindByGuid(dto.UserGuid);
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


                // RATING
                // POST /api/ratings
                case "POST" when path == "/api/ratings/add":
                    {
                        var dto = await Body<RatingCreateDto>(req);
                        var (msg, code, err) = _ratings.Create(dto!);
                        await Send(res, code, err is null ? new { message = msg } : new { error = err });
                        break;
                    }

                // GET /api/ratings/media/{guid}
                case "GET" when path.StartsWith("/api/ratings/media/") && !path.EndsWith("/avg"):
                    {
                        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3 || !Guid.TryParse(parts[3], out var mg))
                        { await Send(res, 400, new { error = "Invalid media guid" }); break; }

                        var (items, code, err) = _ratings.GetForMedia(mg);
                        await Send(res, code, err is null ? items : new { error = err });
                        break;
                    }

                // GET /api/ratings/media/{guid}/avg
               

                // DELETE /api/ratings?user={u}&media={m}
                case "DELETE" when path == "/api/ratings":
                    {
                        if (!Guid.TryParse(req.QueryString["user"], out var ug) ||
                            !Guid.TryParse(req.QueryString["media"], out var mg))
                        { await Send(res, 400, new { error = "Invalid query" }); break; }

                        var (msg, code, err) = _ratings.Delete(ug, mg);
                        await Send(res, code, err is null ? new { message = msg } : new { error = err });
                        break;
                    }


                default:
                    /* ----------------------------------------------
                       SCHRITT: Unbekannte Route
                       EINFACH ERKLÄRT:
                       - Pfad/Methode wurden nicht gefunden.
                       - Wir antworten mit 404 und einer kurzen Info.
                       ---------------------------------------------- */
                    await Send(res, 404, Error("Invalid Path."));
                    break;
            }
        }
        catch (Exception ex)
        {
            /* ============================================================
               SCHRITT: Allgemeine Fehlerbehandlung
               EINFACH ERKLÄRT:
               - Falls unterwegs etwas schiefgeht, loggen wir die
                 Fehlermeldung in Rot zur Konsole.
               - Danach senden wir eine generische 500-Antwort,
                 damit der Client weiß: Serverfehler.
               ============================================================ */
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERR " + ex.Message);
            Console.ResetColor();
            await Send(ctx.Response, 500, Error("Internal Server Error"));
        }

        // ---- local helpers ----

        /* ============================================================
           HELFER: Body<T>
           EINFACH ERKLÄRT:
           - Liest den kompletten Request-Body als Text.
           - Wenn leer, gibt 'default' zurück (also null für Referenztypen).
           - Sonst wird der Text per JSON in den gewünschten Typ T
             umgewandelt (z.B. UserDto).
           ============================================================ */
        async Task<T?> Body<T>(HttpListenerRequest r)
        {
            using var sr = new StreamReader(r.InputStream, r.ContentEncoding);
            var s = await sr.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(s) ? default : JsonSerializer.Deserialize<T>(s, _json);
        }

        /* ============================================================
           HELFER: CheckToken
           EINFACH ERKLÄRT:
           - Erwartet im Header "Authorization: Bearer <token>".
           - Der Token hat ein sehr simples, projektinternes Format:
               mrpx.<UserGuidImFormatN>.<...>.<...>
             (wir prüfen "mrpx" + gültige GUID im "N"-Format).
           - Danach wird in der DB geschaut, ob es den User gibt.
           - Rückgabe:
               (true, username)   -> Token gültig, gehört zu diesem Nutzer
               (false, null)      -> ungültig/nicht vorhanden
           ============================================================ */
        (bool ok, string? username) CheckToken(HttpListenerRequest r)
        {
            var auth = r.Headers["Authorization"];
            if (auth is null || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return (false, null);

            var parts = auth[7..].Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || !parts[0].Equals("mrpx", StringComparison.OrdinalIgnoreCase)) return (false, null);

            if (!Guid.TryParseExact(parts[1], "N", out var userGuid)) return (false, null);
            var u = _db.Users_FindByGuid(userGuid);
            return u is null ? (false, null) : (true, u.Username);
        }

        /* ============================================================
           HELFER: Error
           EINFACH ERKLÄRT:
           - Baut eine einheitliche Fehlerstruktur:
             { "error": "<Nachricht>" }
           - So sehen alle Fehler gleich aus.
           ============================================================ */
        object Error(string msg) => new { error = msg };

        /* ============================================================
           HELFER: Send
           EINFACH ERKLÄRT:
           - Setzt CORS-Header.
           - Serialisiert das Antwortobjekt als JSON.
           - Schreibt Statuscode, Content-Type und Länge.
           - Sendet die Bytes und schließt die Response sauber.
           ============================================================ */
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

        /* ============================================================
           HELFER: Cors
           EINFACH ERKLÄRT:
           - Erlaubt Anfragen von jeder Herkunft (Origin = *).
           - Erlaubt die Methoden GET/POST/PUT/DELETE/OPTIONS.
           - Erlaubt die Header "Content-Type" und "Authorization".
           - Kurz: Der Browser darf mit uns sprechen – auch von
             anderen Domains aus (für einfache Tests sehr praktisch).
           ============================================================ */
        void Cors(HttpListenerResponse r)
        {
            r.Headers["Access-Control-Allow-Origin"] = "*";
            r.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
            r.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        }
    }
}
