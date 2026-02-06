using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class BadRequestException : Exception
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;

        public BadRequestException() : base()
        {
        }

        public BadRequestException(string[] failures)
         : this()
        {
            Errors = failures;
        }

        public  string[] Errors { get; }

        public BadRequestException(string message) : base(message)
        {
        }
    }
}