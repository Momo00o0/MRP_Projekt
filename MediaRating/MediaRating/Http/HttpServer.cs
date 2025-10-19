using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Http
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new();
        private readonly Func<HttpListenerContext, Task> _handler;

        public HttpServer(string prefix, Func<HttpListenerContext, Task> handler)
        {
            _listener.Prefixes.Add(prefix);
            _handler = handler;
        }

        public async Task RunAsync()
        {
            _listener.Start();
            Console.WriteLine($"Listening on {string.Join(", ", _listener.Prefixes)}");
            while (true)
            {
                var ctx = await _listener.GetContextAsync();
                _ = Task.Run(() => _handler(ctx));
            }
        }
    }
}
