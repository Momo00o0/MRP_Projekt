using System.Net;
using MediaRating.Infrastructure;
using MediaRating.Api.Services;

var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:5000/"); 
listener.Start();

var db = new DbContext();          
var http = new HttpService(db);

Console.WriteLine("Listening on http://localhost:5000/");
while (true)
{
    var ctx = await listener.GetContextAsync();
    _ = Task.Run(() => http.HandleRequest(ctx));
}
