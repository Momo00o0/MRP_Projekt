using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Model
{
    public abstract class MediaEntry
    {
#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Fügen Sie ggf. den „erforderlichen“ Modifizierer hinzu, oder deklarieren Sie den Modifizierer als NULL-Werte zulassend.
        public MediaEntry() { }
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Fügen Sie ggf. den „erforderlichen“ Modifizierer hinzu, oder deklarieren Sie den Modifizierer als NULL-Werte zulassend.
        protected MediaEntry(string title, string description, int releaseYear, int ageRestriction, User creator)
        {
            Title = title;
            Description = description;
            ReleaseYear = releaseYear;
            AgeRestriction = ageRestriction;
            Creator = creator;
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ReleaseYear { get; set; }
        public List<string> Genres { get; set; } = new();
        public int AgeRestriction { get; set; }

        public User Creator { get; }
        public List<Rating> Ratings = new();
        public List<User> FavoritedBy { get; set; } = new();
        public Guid Guid { get; set; }
        public double GetAverageScore()
        {
            return Ratings.Select(r => r.Stars).DefaultIfEmpty(0).Average();
        }

    }
}
