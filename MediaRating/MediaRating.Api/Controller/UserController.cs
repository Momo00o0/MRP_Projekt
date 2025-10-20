using System;
using System.Collections.Generic;
using System.Linq;
using MediaRating.Api.DTOs;
using MediaRating.Infrastructure;
using MediaRating.Model;
using MediaRating.Api.Cmd;
using MediaRating.Api.Services;


namespace MediaRating.Api.Controller
{
    public class UserController
    {
        private readonly DbContext _db;
        private HttpService _httpService;

        private int NextId;

         public UserController(DbContext db, HttpService httpService)
        {
            _db = db;
            _httpService = httpService;
            NextId = _db.Users.Count == 0 ? 0 : _db.Users.Max(u => u.Id) + 1;
        }

        public UserController(DbContext db)
        {
            _db = db;
        }

        public (List<User> users, int statusCode, string? errorMessage) GetAllUsers()
        {
            try
            {
                var users = _db.Users.ToList();
                return (users, 200, null);
            }
            catch
            {
                return (new List<User>(), 500, "Failed to retrieve users.");
            }
        }

        public (User? user, int statusCode, string? errorMessage) AddUser(UserDto userData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userData.Username) || string.IsNullOrWhiteSpace(userData.Password))
                    return (null, 400, "Username and password are required.");

                var exists = _db.Users.Any(u => u.Username.Equals(userData.Username, StringComparison.OrdinalIgnoreCase));
                if (exists)
                    return (null, 400, "User already exists.");

                var user = new User(NextId, userData.Username, "")
                {
                    Guid = userData.Guid ?? Guid.NewGuid()
                };
                user.Password = user.HashPassword(userData.Password);

                _db.Users.Add(user);
                NextId++;

                return (user, 200, null);
                }
            catch
            {
                return (null, 500, "Registration failed");
            }
        }

       public (object? loginResponse, int statusCode, string? errorMessage) Login(UserDto userData)
        {
            try
            {
                if (userData is null || string.IsNullOrWhiteSpace(userData.Username) || string.IsNullOrWhiteSpace(userData.Password))
                    return (null, 400, "Username and password are required.");

                var user = _db.Users.FirstOrDefault(u => u.Username.Equals(userData.Username, StringComparison.OrdinalIgnoreCase));
                if (user is null)
                    return (null, 404, "User Not Found");


                if (!user.ComparePassword(userData.Password))
                    return (null, 403, "Unauthorized");
                string token = CreateToken(user);
                var response = new { user.Id, user.Username, user.Guid, Token = token };
                return (response, 200, null);



            }
            catch
            {
                return (null, 500, "Login failed");
            }
        }

       
        public (User? user, int statusCode, string? errorMessage) UpdateUser(Guid guid, UpdateUserCmd userData)
        {
            try
            {
                if (userData is null)
                    return (null, 400, "Update data is required");

                var user = _db.Users.FirstOrDefault(u => u.Guid == guid);
                if (user is null)
                    return (null, 404, "User Not Found");

                if (userData.Username is not null) user.Username = userData.Username;
                if (userData.Password is not null) user.Password = userData.Password;

                return (user, 200, null);
            }
            catch
            {
                return (null, 500, "Update failed");
            }
        }




       
        public string CreateToken(User user)
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string guidN = user.Guid.ToString("N");           // ohne Bindestriche
            string rand8 = Guid.NewGuid().ToString("N")[..8]; // 8 Hex
            return $"mrpx.{guidN}.{ts}.{rand8}";
        }
    }
}
