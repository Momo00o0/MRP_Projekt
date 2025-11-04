using MediaRating.Api.Cmd;
using MediaRating.Api.Controller;
using MediaRating.Api.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using MediaRating.Api.Services;
using NUnit.Framework;
using System;
using System.Linq;
using Assert = NUnit.Framework.Assert;

namespace MediaRating.Tests
{
    [TestFixture]
    public class ApiTests
    {
        private DbContext _db;
        private UserController _users;
        private MediaController _media;

        [SetUp]
        public void Setup()
        {
           
            _db = new DbContext(withSeed: false);
            var httpService = new HttpService(_db);
            _users = new UserController(_db);
            _media = new MediaController(_db);
        }

        // ---------- USER ----------

        [Test]
        public void Password()
        {
            var user = new User(1,"momo","123");
            Assert.AreEqual(user.ComparePassword("123"),true);
        }

        [Test]
        public void UserLogin()
        {
            var user = new UserDto("momo", "123", null);   
            _users.AddUser(user);
            var (loginResponse, status, error) = _users.Login(user);

            Assert.That(status, Is.EqualTo(200));
            Assert.That(loginResponse, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        public void UserRegister()
        {
            var user = new UserDto("momo", "123", null);
            var (userData, status, error) = _users.AddUser(user);
            Assert.That(status, Is.EqualTo(200));
            Assert.That(userData.Username, Is.EqualTo("momo"));
        }

        [Test]
        public void Update_User_Changes_Username()
        {
            // arrange
            var user = new UserDto("momo", "123", null);
            var (created, _, _) = _users.AddUser(user);

            // act
            var update = new UpdateUserCmd("momo2", null);
            var (updated, status, error) = _users.UpdateUser(created.Guid, update);

            // assert
            Assert.That(status, Is.EqualTo(200));
            Assert.That(error, Is.Null);
            Assert.That(updated!.Username, Is.EqualTo("momo2"));
        }

        [Test]
        public void CreateToken_Has_Expected_Format()
        {
            var user = new UserDto("Mohamad", "123", null);
            var (userData, _, _) = _users.AddUser(user);

            var token = _users.CreateToken(userData);

            Assert.That(token, Does.StartWith("mohamad-mrp-"));
            Assert.That(token.Split('-').Length, Is.EqualTo(4));
        }

        // ---------- MEDIA ----------

        [Test]
        public void Create_Movie()
        {
            // creator anlegen
            var (creator, _, _) = _users.AddUser(new UserDto("creator", "pw", null));

            var m = new MediaEntryDto(
                Title: "Interstellar",
                Description: "Sci-Fi Drama",
                ReleaseYear: 2014,
                Genres: new List<string> { "Sci-Fi", "Drama" },
                AgeRestriction: 12,
                UserGuid: creator.Guid,
                Kind: MediaKind.Movie
            );

            var (entity, status, error) = _media.Create(m, creator);

            Assert.That(status, Is.EqualTo(201));
            Assert.That(error, Is.Null);
            Assert.That(entity, Is.Not.Null);
            Assert.That(_db.MediaEntries.Any(x => x.Guid == entity.Guid), Is.True);

            // Basiseigenschaften
            Assert.That(entity.Title, Is.EqualTo("Interstellar"));
           
        }

        [Test]
        public void GetMedia()
        {
           
            var (creator, _, _) = _users.AddUser(new UserDto("momo", "pw", null));
            var m = new MediaEntryDto("Film A", "desc", 2020, new(), 12, creator.Guid, MediaKind.Movie);
            _media.Create(m, creator);

            // act
            var (items, status, error) = _media.GetAll();

            // assert
            Assert.That(status, Is.EqualTo(200));
            Assert.That(error, Is.Null);
            Assert.That(items.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(items[0].Creator, Is.Not.Null);
        }

        [Test]
        public void GetMediaById()
        {
            var (creator, _, _) = _users.AddUser(new UserDto("momo", "pw", null));
            var dto = new MediaEntryDto("Series X", "desc", 2022, new(), 16, creator!.Guid, MediaKind.Series);
            var (entity, _, _) = _media.Create(dto, creator);

            var (found, status, error) = _media.GetById(entity.Guid);

            Assert.That(status, Is.EqualTo(200));
            Assert.That(error, Is.Null);
            Assert.That(found!.Guid, Is.EqualTo(entity.Guid));
            Assert.That(found.Title, Is.EqualTo("Series X"));
        }
    }
}
