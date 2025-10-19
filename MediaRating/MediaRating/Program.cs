using MediaRating.Services;
using MediaRating.Infrastructure;
using MediaRating.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


var db = new DbContext(withSeed: true);
var server = new HttpService("http://localhost:5000/", db); // oder SimpleHttpServer
await server.StartAsync();



