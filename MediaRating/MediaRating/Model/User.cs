using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
namespace MediaRating.Model
{
    public class User
    {

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Fügen Sie ggf. den „erforderlichen“ Modifizierer hinzu, oder deklarieren Sie den Modifizierer als NULL-Werte zulassend.
        public User() { }
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Fügen Sie ggf. den „erforderlichen“ Modifizierer hinzu, oder deklarieren Sie den Modifizierer als NULL-Werte zulassend.

        public User(int id,string username, string password) {

            Id = id;
            Username = username;
            Password = HashPassword(password);
        }

        public int Id { get; set; } 
        public string Username { get; set; }
        public string Password { get; set; } 
        public List<Rating> Ratings { get; set; } = new();
        public List<MediaEntry> Favorites { get; set; } = new();
        public Guid Guid = Guid.NewGuid();

        public string HashPassword(string password)
        {
            
            var salt = new string(Username.ToUpper().Reverse().ToArray());
            var input = password + salt;

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public bool ComparePassword(string password) => HashPassword(password) == Password;
    }
}
