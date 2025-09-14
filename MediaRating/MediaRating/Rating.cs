using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating
{
    public class Rating
    {
        public int Stars { get; set; } 
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool CommentConfirmed { get; set; }
        public User Creator { get; set; }
        public MediaEntry Media { get; set; }
        public List<User> LikedBy { get; set; } = new();
    }
}
