using MediaRating.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Infrastructure
{
    public class DbContext
    {
        public List<MediaEntry> MediaEntries { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Rating> Ratings { get; set; } = new();

        public bool IsSeeded { get; private set; }

        public DbContext(bool withSeed = true)
        {
            if (withSeed)
            {
                Seed();
            }
            Console.WriteLine("AppMemoryContext ready.");
        }


        public void Seed()
        {
            if (IsSeeded) return;

            Console.WriteLine("Seeding in-memory data");

            User admin = new User(0, "admin", "");
            admin.Password = admin.HashPassword("admin");
            admin.Guid = Guid.NewGuid();

            Users.Add(admin);

            Movie movie1 = new Movie("King Kong", "Scary", 2005, 16, admin);
            Game game = new Game("Fortnite", "Battle Royale", 2017, 12,admin); 
            Series series = new Series("Prison Break", "Action", 2005,18,admin);

            MediaEntries.Add(movie1);
            MediaEntries.Add(game);
            MediaEntries.Add(series);

            IsSeeded = true;
        }

    }
}
