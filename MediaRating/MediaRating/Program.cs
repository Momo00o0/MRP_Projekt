using MediaRating.Http;
using MediaRating.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


var router = new Router();
router.Get("/api/health", async ctx =>        // 2) dann Route registrieren
{
    await router.Json(ctx.Response, 200, new { status = "ok" });
});

var server = new HttpServer("http://localhost:8080/", router.HandleAsync);
await server.RunAsync();



