using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.NotFound;

        public NotFoundException() : base() { }

        public NotFoundException(string message) : base(message) {
        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }

        public NotFoundException(string name, object value) : base($"Entity '{name}' ({value}) was not found.") { }
    }
}