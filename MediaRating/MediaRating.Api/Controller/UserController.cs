using MediaRating.Api.Cmd;
using MediaRating.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using System;
using System.Collections.Generic;

namespace MediaRating.Api.Controller
{
    public class UserController
    {
        private readonly IMediaRatingContext _db;
        public UserController(IMediaRatingContext db) { _db = db; }

        public List<User> GetAllUsers()
        {
            return _db.Users_GetAll();
        }


        public (User? user, int statusCode, string? errorMessage) AddUser(UserDto userData)
        {
            if (string.IsNullOrWhiteSpace(userData.Username) || string.IsNullOrWhiteSpace(userData.Password))
                return (null, 400, "Username and password are required.");

            // exists?
            var exists = _db.Users_FindByUsername(userData.Username);
            if (exists != null) return (null, 409, "User already exists.");

            var tmp = new User(0, userData.Username, "");
            var hash = tmp.HashPassword(userData.Password);

            var u = _db.Users_Insert(userData.Username, hash, userData.Guid);  // <-- nur User
            return (u, 201, null);
        }

        public (object? loginResponse, int statusCode, string? errorMessage) Login(UserDto userData)
        {
            try
            {
                if (userData is null || string.IsNullOrWhiteSpace(userData.Username) || string.IsNullOrWhiteSpace(userData.Password))
                    return (null, 400, "Username and password are required.");

                var user = _db.Users_FindByUsername(userData.Username);
                if (user is null) return (null, 404, "User Not Found");
                if (!user.ComparePassword(user.Password,userData.Password)) return (null, 403, "Unauthorized");

                string token = CreateToken(user);
                var response = new { user.Id, user.Username, user.Guid, Token = token };
                return (new { token = token }, 200, null);
            }
            catch
            {
                return (null, 500, "Login failed");
            }
        }


        public string CreateToken(User user)
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string guidN = user.Guid.ToString("N");
            string rand8 = Guid.NewGuid().ToString("N")[..8];
            return $"mrpx.{guidN}.{ts}.{rand8}";
        }
    }
}
