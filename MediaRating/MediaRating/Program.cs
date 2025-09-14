namespace MediaRating
{
    internal class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Hello!");
            Film MyFilm = new Film();
            MyFilm.Title = "Khamzat";
            MyFilm.Description = "Borz";
            MyFilm.MediaType = MediaType.Video;
            MyFilm.ReleaseYear = 2017;
            Console.WriteLine($"{MyFilm.Title} {MyFilm.Description}");

        }
    }
}