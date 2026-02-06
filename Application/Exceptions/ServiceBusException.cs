using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class ServiceBusException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.ServiceUnavailable;
        public ServiceBusException() : base()
        {
        }
 
        public ServiceBusException(string message) : base(message)
        {
        }
    }
}