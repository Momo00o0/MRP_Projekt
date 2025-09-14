using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating
{
    public abstract class MediaEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int ReleaseYear { get; set; }
        public List<string> Genres { get; set; } = new();
        public int AgeRestriction { get; set; }

        public User Creator { get; }
        public List<Rating> Ratings { get; set; } = new();
        public List<User> FavoritedBy { get; set; } = new();

        public double AverageScore { get; set; }

    }
}
