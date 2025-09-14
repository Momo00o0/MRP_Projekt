using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Model
{
    public class Rating
    {

#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Fügen Sie ggf. den „erforderlichen“ Modifizierer hinzu, oder deklarieren Sie den Modifizierer als NULL-Werte zulassend.
        public Rating() { }
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Fügen Sie ggf. den „erforderlichen“ Modifizierer hinzu, oder deklarieren Sie den Modifizierer als NULL-Werte zulassend.

        public Rating(int stars, string? comment, DateTime timeStamp, bool confirmed, User creator, MediaEntry media) {
        
            Stars = stars;
            Comment = comment;
            Timestamp = timeStamp;
            Confirmed = confirmed;
            Creator = creator;
            Media = media;
        
        }

        public int Stars { get; set; } 
        public string? Comment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Confirmed { get; set; }
        public User Creator { get; set; }
        public MediaEntry Media { get; set; }
        public List<User> LikedBy { get; set; } = new();
    }
}
