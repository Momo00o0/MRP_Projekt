using System.Net;
using MediaRating.Infrastructure;
using MediaRating.Api.Services;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // Connection-String prüfen (ENV: PG_CONN)
        var rawConn = Environment.GetEnvironmentVariable("PG_CONN");
        if (string.IsNullOrWhiteSpace(rawConn))
        {

            Console.WriteLine("PG_CONN ist nicht gesetzt. Bitte zuerst setzen!");

            return;
        }
        Console.WriteLine("PG_CONN gefunden.");

        // DB + HttpService bauen
        var db = new MediaRatingContext();          
        var http = new HttpService(db);     

        // HttpListener vorbereiten
        using var listener = new HttpListener();
        
        listener.Prefixes.Add("http://localhost:8080/");
        
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine("HttpListener konnte nicht starten: " + ex.Message);
            return;
        }

        Console.WriteLine("Listening on http://localhost:8080/");

        while (true)
        {
            var ctx = await listener.GetContextAsync();
            _ = Task.Run(() => http.HandleRequest(ctx)); 
        }

    }
}
