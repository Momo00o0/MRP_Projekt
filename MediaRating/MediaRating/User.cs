using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; } 
        public List<Rating> Ratings { get; set; } = new();
        public List<MediaEntry> Favorites { get; set; } = new();
    }
}
