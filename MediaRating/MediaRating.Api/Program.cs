using System.Net;
using MediaRating.Infrastructure;
using MediaRating.Api.Services;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // 1) Connection-String prüfen (ENV: PG_CONN)
        var rawConn = Environment.GetEnvironmentVariable("PG_CONN");
        if (string.IsNullOrWhiteSpace(rawConn))
        {
           
            Console.WriteLine("PG_CONN ist nicht gesetzt. Bitte zuerst setzen!");
           
            return;
        }
        // nur fürs Log: Passwort maskieren
        Console.WriteLine("PG_CONN gefunden.");

        // 2) DB + HttpService bauen
        var db = new MediaRatingContext();          
        var http = new HttpService(db);     

        // 3) HttpListener vorbereiten
        using var listener = new HttpListener();
        
        listener.Prefixes.Add("http://localhost:8080/");
        
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("HttpListener konnte nicht starten: " + ex.Message);
            Console.WriteLine("Falls 'Access is denied':");
            Console.WriteLine("  → PowerShell als Administrator öffnen und ausführen:");
            Console.WriteLine(@"    netsh http add urlacl url=http://+:8080/ user=DEINBENUTZERNAME");
            Console.ResetColor();
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
