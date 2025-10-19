using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaRating.Services
{
    public class HttpError : Exception
    {
        public int Status { get; }
        public HttpError(int status, string message) : base(message) { Status = status; }
    }
}
