using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.Data.Sqlite;
using MediaRating.Model;

namespace MediaRating.Tests
{
    public class Tests
    {

        [Fact]
        public void CreateUser()
        {

            var user = new User("Richard", "111");
           Assert.True(user.Username == "Richard");
        }
        [Fact]
        public void CreateMovie()
        {

            var user = new User("David", "123");

            var movie= new Movie("King Kong", "scary",2005, 16, user, 0);


            Assert.True(movie.Creator.Equals(user) && movie.Title == "King Kong");
        }

        [Fact]
        public void CreateRating() {

            var user = new User("David", "123");
            var user2 = new User("Richard", "1213232");
            var movie = new Movie("King Kong", "scary", 2005, 16, user, 0);
            var rating = new Rating(4, "Sehr schön", DateTime.Now, false, user, movie);

            movie.Ratings.Add(rating);
            user.Ratings.Add(rating);
            rating.LikedBy.Add(user2);

            Assert.True(movie.Ratings.Contains(rating)&& user.Ratings.Contains(rating)&&rating.LikedBy.Contains(user2));


        }
    }
}
