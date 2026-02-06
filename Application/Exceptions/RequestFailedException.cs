using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class RequestFailedException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.ServiceUnavailable;
        public RequestFailedException() : base()
        {
        }
 
        public RequestFailedException(string message) : base(message)
        {
        }
    }
}