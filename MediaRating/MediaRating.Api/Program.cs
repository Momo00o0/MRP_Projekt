using System.Net;
using MediaRating.Infrastructure;
using MediaRating.Api.Services;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();

        var db = new DbContext();
        var http = new HttpService(db);

        Console.WriteLine("Listening on http://localhost:8080/");
        while (true)
        {
            var ctx = await listener.GetContextAsync();
            _ = Task.Run(() => http.HandleRequest(ctx));
        }
    }
}
