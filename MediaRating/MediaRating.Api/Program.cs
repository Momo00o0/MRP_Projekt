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
        var db = new MediaRatingContext();          // <-- ADO.NET (Npgsql) – deine einfache Infrastruktur
        var http = new HttpService(db);     // <-- dein HttpService wurde ja auf SimpleDb umgestellt

        // 3) HttpListener vorbereiten
        using var listener = new HttpListener();
        // Tipp: auf Windows funktioniert oft "http://+:8080/" besser als localhost
        listener.Prefixes.Add("http://localhost:8080/");
        // listener.Prefixes.Add("http://+:8080/"); // Alternative (siehe Hinweis unten)
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
        Console.WriteLine("Beenden mit STRG + C");

        // 4) Graceful shutdown via Ctrl+C
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // 5) Request-Loop
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var ctx = await listener.GetContextAsync();
                _ = Task.Run(() => http.HandleRequest(ctx), cts.Token);
            }
        }
        catch (OperationCanceledException) { /* normal bei Ctrl+C */ }
        finally
        {
            if (listener.IsListening) listener.Stop();
        }
    }
}
