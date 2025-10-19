using MediaRating.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MediaRating.Http
{
    public class Router
    {
        private readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // Sehr einfaches Routing: (Method, Path) → Handler
        private readonly Dictionary<(string method, string path), Func<HttpListenerContext, Task>> _routes = new();

        public Router Get(string path, Func<HttpListenerContext, Task> h)
        { _routes[("GET", path)] = h; return this; }

        public async Task HandleAsync(HttpListenerContext ctx)
        {
            try
            {
                var key = (ctx.Request.HttpMethod, ctx.Request.Url!.AbsolutePath);
                if (_routes.TryGetValue(key, out var h))
                    await h(ctx);
                else
                    await Json(ctx.Response, 404, new { error = "Not found" });
            }
            catch (HttpError ex) { await Json(ctx.Response, ex.Status, new { error = ex.Message }); }
            catch (Exception ex) { await Json(ctx.Response, 500, new { error = ex.Message }); }
        }

        // Hilfen für JSON I/O (brauchst du später überall)
        public async Task Json(HttpListenerResponse res, int status, object payload)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, _json));
            res.StatusCode = status;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            res.Close();
        }
    }
}
