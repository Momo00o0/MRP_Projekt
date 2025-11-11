using MediaRating.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaRating.DTOs;

namespace MediaRating.Infrastructure
{
    public class MediaRatingContext
    {
        // Liest den Connection String genau EINMAL beim Laden der Klasse.
        // Wenn die Umgebungsvariable PG_CONN fehlt, werfen wir eine klare Exception.
        private static readonly string Conn =
            Environment.GetEnvironmentVariable("PG_CONN")
            ?? throw new InvalidOperationException("PG_CONN not set.");

        // Kleine Hilfsfunktion: macht eine neue DB-Verbindung auf und gibt sie geöffnet zurück.
        // Vorteil: In jeder Methode reicht "using var con = Open();" und wir haben eine fertige Verbindung.
        private static NpgsqlConnection Open()
        {
            var c = new NpgsqlConnection(Conn);
            c.Open();
            return c;
        }

     
        //            USERS
      
        // Holt ALLE Benutzer aus der Tabelle "users".
        // Mapped jede DB-Zeile auf ein User-Objekt (Id, Guid, Username, Password-Hash).
        public List<User> Users_GetAll()
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, guid, username, password_hash FROM users ORDER BY id;", con);
            using var rd = cmd.ExecuteReader();

            var list = new List<User>();
            while (rd.Read())
            {
                // Spalten-Indexe: 0=id, 1=guid, 2=username, 3=password_hash
                var u = new User(rd.GetInt32(0), rd.GetString(2), "") { Guid = rd.GetGuid(1) };
                u.Password = rd.GetString(3); 
                list.Add(u);
            }
            return list;
        }

        // Sucht einen Benutzer per "username".
        // Gibt null zurück, wenn kein Treffer vorhanden ist.
        public User? Users_FindByUsername(string username)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, guid, username, password_hash FROM users WHERE username=@u;", con);
            cmd.Parameters.AddWithValue("@u", username);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;

            var u = new User(rd.GetInt32(0), rd.GetString(2), "") { Guid = rd.GetGuid(1) };
            u.Password = rd.GetString(3);
            return u;
        }

        // Sucht einen Benutzer per "guid".
        // Gibt null zurück, wenn kein Treffer vorhanden ist.
        public User? Users_FindByGuid(Guid guid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, guid, username, password_hash FROM users WHERE guid=@g;", con);
            cmd.Parameters.AddWithValue("@g", guid);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;

            var u = new User(rd.GetInt32(0), rd.GetString(2), "") { Guid = rd.GetGuid(1) };
            u.Password = rd.GetString(3);
            return u;
        }

        // Legt einen neuen Benutzer in der DB an.
        // - username: gewünschter Benutzername
        // - passwordHash: bereits gehashter Passwort-String
        // - fixedGuid: optionaler fixer Guid (z.B. für Tests); sonst wird ein neuer Guid erzeugt.
        // Rückgabe: der gerade eingefügte User mit DB-Id und Guid.
        public User Users_Insert(string username, string passwordHash, Guid? fixedGuid = null)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (guid, username, password_hash) VALUES (@g,@u,@p) RETURNING id, guid;", con);

            var g = fixedGuid ?? Guid.NewGuid();    // falls nicht vorgegeben, neuen Guid erzeugen
            cmd.Parameters.AddWithValue("@g", g);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);

            // RETURNING liefert uns direkt id + guid der neuen Zeile.
            using var rd = cmd.ExecuteReader();
            rd.Read();

            return new User(rd.GetInt32(0), username, "")
            {
                Guid = rd.GetGuid(1),
                Password = passwordHash
            };
        }


      
        //           MEDIA

        // Holt ALLE Media-Einträge (Movie/Series/Game) inklusive Ersteller (User).
        // Mapping:
        //   - kind steuert, welche konkrete Klasse instanziiert wird (Movie/Series/Game).
        //   - description kann in der DB NULL sein → rd.IsDBNull(2) prüfen und entsprechend zuweisen.
        public List<MediaEntry> Media_GetAll()
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT m.guid, m.title, m.description, m.release_year, m.age_restriction, m.kind,
                       u.id, u.guid, u.username
                  FROM media_entries m
                  JOIN users u ON u.id = m.creator_id
                 ORDER BY m.id;", con);
            using var rd = cmd.ExecuteReader();

            var list = new List<MediaEntry>();
            while (rd.Read())
            {
                // Creator-User aus den Spalten 6..8 aufbauen
                var creator = new User(rd.GetInt32(6), rd.GetString(8), "") { Guid = rd.GetGuid(7) };

                // Art des Media-Eintrags (Movie/Series/Game)
                var kind = rd.GetString(5);

                // Instanz passend zum kind erzeugen.
                // description (Spalte 2) kann NULL sein → rd.IsDBNull(2) prüfen.
                MediaEntry m = kind switch
                {
                    "Movie" => new Movie(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator),
                    "Series" => new Series(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator),
                    "Game" => new Game(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator),
                    _ => new Movie(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator)
                };

                // Den Guid des Media-Eintrags setzen (Spalte 0).
                m.Guid = rd.GetGuid(0);

                list.Add(m);
            }
            return list;
        }

        // Holt GENAU EINEN Media-Eintrag per Guid (inkl. Creator).
        // Gibt null zurück, wenn nichts gefunden wird.
        public MediaEntry? Media_GetByGuid(Guid guid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT m.guid, m.title, m.description, m.release_year, m.age_restriction, m.kind,
                       u.id, u.guid, u.username
                  FROM media_entries m
                  JOIN users u ON u.id = m.creator_id
                 WHERE m.guid=@g;", con);
            cmd.Parameters.AddWithValue("@g", guid);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;

            var creator = new User(rd.GetInt32(6), rd.GetString(8), "") { Guid = rd.GetGuid(7) };
            var kind = rd.GetString(5);

            MediaEntry m = kind switch
            {
                "Movie" => new Movie(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator),
                "Series" => new Series(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator),
                "Game" => new Game(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator),
                _ => new Movie(rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2), rd.GetInt32(3), rd.GetInt32(4), creator)
            };
            m.Guid = rd.GetGuid(0);
            return m;
        }

        // Erstellt einen neuen Media-Eintrag in der DB.
        // ACHTUNG (Design-Hinweis): Hier wird ein DTO (MediaEntryDto) in der Infrastruktur benutzt.
        // Besser wäre: die Controller validieren + übergeben primitive Parameter (Title, Year, ...),
        // und die Infrastruktur macht nur das INSERT. Für jetzt lassen wir es so, damit es läuft.
        public MediaEntry Media_Create(MediaEntryDto dto, User creator)
        {
            var g = Guid.NewGuid(); // neuer Guid für den Media-Eintrag

            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO media_entries (guid, title, description, release_year, age_restriction, kind, creator_id)
                VALUES (@g,@t,@d,@y,@a,@k,@c);", con);

            // Parameter binden (description darf NULL sein → DBNull.Value)
            cmd.Parameters.AddWithValue("@g", g);
            cmd.Parameters.AddWithValue("@t", dto.Title);
            cmd.Parameters.AddWithValue("@d", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@y", dto.ReleaseYear);
            cmd.Parameters.AddWithValue("@a", dto.AgeRestriction);
            cmd.Parameters.AddWithValue("@k", dto.Kind.ToString());
            cmd.Parameters.AddWithValue("@c", creator.Id);
            cmd.ExecuteNonQuery(); // INSERT ausführen

            // Passendes Domain-Objekt zurückgeben (inkl. gesetztem Guid).
            MediaEntry entity = dto.Kind switch
            {
                MediaKind.Movie => new Movie(dto.Title, dto.Description!, dto.ReleaseYear, dto.AgeRestriction, creator),
                MediaKind.Series => new Series(dto.Title, dto.Description!, dto.ReleaseYear, dto.AgeRestriction, creator),
                MediaKind.Game => new Game(dto.Title, dto.Description!, dto.ReleaseYear, dto.AgeRestriction, creator),
                _ => new Movie(dto.Title, dto.Description!, dto.ReleaseYear, dto.AgeRestriction, creator)
            };
            entity.Guid = g;
            return entity;
        }

        // RATINGS
        private int RequireUserId(NpgsqlConnection con, Guid userGuid)
        {
            using var c = new NpgsqlCommand("SELECT id FROM users WHERE guid=@g;", con);
            c.Parameters.AddWithValue("@g", userGuid);
            var r = c.ExecuteScalar();
            if (r is null) throw new InvalidOperationException("User not found");
            return Convert.ToInt32(r);
        }

        private int RequireMediaId(NpgsqlConnection con, Guid mediaGuid)
        {
            using var c = new NpgsqlCommand("SELECT id FROM media_entries WHERE guid=@g;", con);
            c.Parameters.AddWithValue("@g", mediaGuid);
            var r = c.ExecuteScalar();
            if (r is null) throw new InvalidOperationException("Media not found");
            return Convert.ToInt32(r);
        }

        // Prüfen, ob bereits ein Rating für (user, media) existiert
        public bool Ratings_Exists(Guid userGuid, Guid mediaGuid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT 1
                  FROM ratings r
                  JOIN users u ON u.id = r.user_id
                  JOIN media_entries m ON m.id = r.media_id
                 WHERE u.guid=@ug AND m.guid=@mg
                 LIMIT 1;", con);
            cmd.Parameters.AddWithValue("@ug", userGuid);
            cmd.Parameters.AddWithValue("@mg", mediaGuid);
            return cmd.ExecuteScalar() != null;
        }

        // CREATE: neues Rating anlegen (wir verlassen uns auf den Unique-Index für Kollisionen)
        public void Ratings_Insert(Guid userGuid, Guid mediaGuid, int stars, string? comment)
        {
            using var con = Open();
            var userId = RequireUserId(con, userGuid);
            var mediaId = RequireMediaId(con, mediaGuid);

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO ratings (user_id, media_id, stars, comment)
                VALUES (@u, @m, @s, @c);", con);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@m", mediaId);
            cmd.Parameters.AddWithValue("@s", stars);
            cmd.Parameters.AddWithValue("@c", (object?)comment ?? DBNull.Value);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // unique_violation
            {
                // bereits vorhanden
                throw new InvalidOperationException("Rating already exists");
            }
        }

        // READ: alle Ratings zu einem Media (einfaches Mapping in deine Rating-Klasse)
        public List<Rating> Ratings_GetForMedia(Guid mediaGuid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT r.stars, r.comment,
                       u.id   AS uid,   u.guid AS uguid,   u.username,
                       m.id   AS mid,   m.guid AS mguid,   m.title
                  FROM ratings r
                  JOIN users u ON u.id = r.user_id
                  JOIN media_entries m ON m.id = r.media_id
                 WHERE m.guid=@g
                 ORDER BY r.id DESC;", con);
            cmd.Parameters.AddWithValue("@g", mediaGuid);

            using var rd = cmd.ExecuteReader();
            var list = new List<Rating>();
            while (rd.Read())
            {
                var creator = new User(rd.GetInt32(rd.GetOrdinal("uid")),
                                       rd.GetString(rd.GetOrdinal("username")), "")
                { Guid = rd.GetGuid(rd.GetOrdinal("uguid")) };

                // simples Media-Objekt (Titel reicht meistens für Anzeige)
                var mediaOwner = creator; // creator des Media-Eintrags ist bei dir ohnehin separat vorhanden
                var media = new Movie(rd.GetString(rd.GetOrdinal("title")), null, 0, 0, mediaOwner);

                var rating = new Rating(
                    stars: rd.GetInt32(rd.GetOrdinal("stars")),
                    comment: rd.IsDBNull(rd.GetOrdinal("comment")) ? null : rd.GetString(rd.GetOrdinal("comment")),
                    timeStamp: DateTime.UtcNow,   // DB hat kein timestamp-Feld
                    confirmed: false,             // DB hat kein confirmed
                    creator: creator,
                    media: media
                );

                list.Add(rating);
            }
            return list;
        }

        // (optional) READ: alle Ratings eines Users
        public List<Rating> Ratings_GetForUser(Guid userGuid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT r.stars, r.comment,
                       u.id AS uid, u.guid AS uguid, u.username,
                       m.id AS mid, m.guid AS mguid, m.title
                  FROM ratings r
                  JOIN users u ON u.id = r.user_id
                  JOIN media_entries m ON m.id = r.media_id
                 WHERE u.guid=@g
                 ORDER BY r.id DESC;", con);
            cmd.Parameters.AddWithValue("@g", userGuid);

            using var rd = cmd.ExecuteReader();
            var list = new List<Rating>();
            while (rd.Read())
            {
                var creator = new User(rd.GetInt32(rd.GetOrdinal("uid")),
                                       rd.GetString(rd.GetOrdinal("username")), "")
                { Guid = rd.GetGuid(rd.GetOrdinal("uguid")) };

                var media = new Movie(rd.GetString(rd.GetOrdinal("title")), null, 0, 0, creator);
                var rating = new Rating(
                    stars: rd.GetInt32(rd.GetOrdinal("stars")),
                    comment: rd.IsDBNull(rd.GetOrdinal("comment")) ? null : rd.GetString(rd.GetOrdinal("comment")),
                    timeStamp: DateTime.UtcNow,
                    confirmed: false,
                    creator: creator,
                    media: media
                );
                list.Add(rating);
            }
            return list;
        }

        // READ: Durchschnitt für ein Media (null wenn keine Ratings)
        public double? Media_GetAverageScore(Guid mediaGuid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT AVG(stars)::float
                  FROM ratings r
                  JOIN media_entries m ON m.id = r.media_id
                 WHERE m.guid=@g;", con);
            cmd.Parameters.AddWithValue("@g", mediaGuid);
            var r = cmd.ExecuteScalar();
            return r is DBNull or null ? (double?)null : Convert.ToDouble(r);
        }

        // DELETE: Rating des Users für ein Media (falls mehrere in DB erlaubt wären, löschen wir alle)
        public int Ratings_Delete(Guid userGuid, Guid mediaGuid)
        {
            using var con = Open();
            using var cmd = new NpgsqlCommand(@"
                DELETE FROM ratings
                 WHERE user_id  = (SELECT id FROM users WHERE guid=@ug)
                   AND media_id = (SELECT id FROM media_entries WHERE guid=@mg);", con);
            cmd.Parameters.AddWithValue("@ug", userGuid);
            cmd.Parameters.AddWithValue("@mg", mediaGuid);
            return cmd.ExecuteNonQuery(); // Anzahl gelöschter Zeilen
        }
    }
}
