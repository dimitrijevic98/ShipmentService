using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Exceptions;

namespace Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                response.ContentType = "application/json";

                // _logger.LogError("Error: " + error.Message);
                // _logger.LogError("Error Details: " + context.Request.Path);

                // if (error.InnerException != null)
                //     _logger.LogError("Inner Error: " + error.InnerException.Message);



                // var errors = new Dictionary<string, string[]>();
                Dictionary<string, string[]>? errors = null;
                switch (error)
                {
                    case NotFoundException:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                    case BadRequestException bre:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        break;
                    case RequestFailedException:
                        response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                        break;
                    case ServiceBusException:
                        response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                        break; 
                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                _logger.LogError(
                    "{ExceptionType} on {Method} {Path}. StatusCode: {StatusCode}, Message: {Error}, Inner: {Inner}, TraceId: {TraceId}",
                    error.GetType().Name,
                    context.Request.Method,
                    context.Request.Path,
                    response.StatusCode,
                    error.Message,
                    error.InnerException?.Message,
                    context.TraceIdentifier
                );

                var result = JsonSerializer.Serialize(new ErrorDetails
                {
                    StatusCode = response.StatusCode,
                    Message = error.Message,
                    Errors = errors
                });

                await response.WriteAsync(result);
            }
        }
    }
}