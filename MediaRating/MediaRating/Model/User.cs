using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using MediaRating.PasswordHash;
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
            string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password, 13); //Cost Parameter
            return hashedPassword;
        }

        public bool ComparePassword(string hash, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            return BCrypt.Net.BCrypt.EnhancedVerify(password,hash);
        }

    }
}
