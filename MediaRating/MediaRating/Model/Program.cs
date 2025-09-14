namespace MediaRating.Model
{
    internal class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Hello!");
            Movie MyFilm = new Movie();
            MyFilm.Title = "King Kong";
            MyFilm.Description = "Scary";
            MyFilm.ReleaseYear = 2017;
            Console.WriteLine($"{MyFilm.Title} {MyFilm.Description} {MyFilm.ReleaseYear}");

        }
    }
}