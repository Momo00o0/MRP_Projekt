using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Model
{
    public class Movie: MediaEntry
    {

        public Movie() { }
        public Movie(string title, string description, int releaseYear, int ageRestriction, User creator)
        : base(title, description, releaseYear, ageRestriction, creator) { }
    }
}
