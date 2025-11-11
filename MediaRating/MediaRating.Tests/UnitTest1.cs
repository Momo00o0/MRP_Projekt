using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

using MediaRating.Infrastructure;
using MediaRating.Api.Controller;
using MediaRating.DTOs;
using MediaRating.Model;

namespace MediaRating.Tests
{
    [TestFixture]
    public class ApiBasicTests
    {
        private string _conn = null!;
        private MediaRatingContext _db = null!;
        private UserController _users = null!;
        private MediaController _media = null!;
        private RatingController _ratings = null!;

        [OneTimeSetUp]
        public void OneTime()
        {
            _conn = Environment.GetEnvironmentVariable("PG_CONN")
                ?? throw new InvalidOperationException("PG_CONN fehlt (Tests).");
        }

        [SetUp]
        public void Setup()
        {
            // DB sauber machen vor jedem Test
            using (var con = new NpgsqlConnection(_conn))
            {
                con.Open();
                using var cmd = new NpgsqlCommand(
                    "TRUNCATE TABLE ratings, media_entries, users RESTART IDENTITY CASCADE;", con);
                cmd.ExecuteNonQuery();
            }

            _db = new MediaRatingContext();
            _users = new UserController(_db);
            _media = new MediaController(_db);
            _ratings = new RatingController(_db);
        }

        // -------- USER --------

        [Test]
        public void Password_Hash_Compare_Works()
        {
            var u = new User(1, "momo", "");
            u.Password = u.HashPassword("123");

            // Prüft, dass das Passwort "123" gegen den gespeicherten Hash korrekt erkannt wird (true).
            Assert.That(u.ComparePassword("123")); // EIN Assert
        }

        [Test]
        public void CreateToken_Format()
        {
            var (user, _, _) = _users.AddUser(new UserDto("tokuser", "pw", null));
            var token = _users.CreateToken(user!);

            // Prüft, dass das erzeugte Token mit dem erwarteten Präfix beginnt ("mrpx.").
            Assert.That(token.StartsWith("mrpx.")); // EIN Assert
        }

        [Test]
        public void Register_Then_Login()
        {
            _users.AddUser(new UserDto("momo", "123", null));
            var (login, _, _) = _users.Login(new UserDto("momo", "123", null));

            // Prüft, dass nach Registrierung ein erfolgreicher Login möglich ist (es gibt eine Login-Response).
            Assert.That(login, Is.Not.Null); // EIN Assert
        }

        // -------- MEDIA --------

        [Test]
        public void Create_Media_And_List()
        {
            var (creator, _, _) = _users.AddUser(new UserDto("creator", "pw", null));

            var dto = new MediaEntryDto(
                Title: "Interstellar",
                Description: "Sci-Fi Drama",
                ReleaseYear: 2014,
                Genres: new List<string> { "Sci-Fi", "Drama" },
                AgeRestriction: 12,
                UserGuid: creator!.Guid,
                Kind: MediaKind.Movie
            );

            var (entity, _, _) = _media.Create(dto, creator);
            var (all, _, _) = _media.GetAll();

            // Prüft, dass der eben erstellte Media-Eintrag in der Liste aller Medien auftaucht (per Guid).
            Assert.That(all.Any(m => m.Guid == entity.Guid)); // EIN Assert
        }

        [Test]
        public void Get_Media_By_Id()
        {
            var (creator, _, _) = _users.AddUser(new UserDto("c", "pw", null));
            var (created, _, _) = _media.Create(
                new MediaEntryDto("Series X", "desc", 2022, new List<string> { "Action" }, 16, creator!.Guid, MediaKind.Series),
                creator
            );

            var (found, _, _) = _media.GetById(created.Guid);

            // Prüft, dass ein Media-Eintrag mit der erstellten Guid aus der DB gelesen werden kann (nicht null).
            Assert.That(found, Is.Not.Null); // EIN Assert
        }

        
    }
}
