using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace RailwayReservationSystem.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        public GlobalExceptionHandler(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            try { await _next(context); }
            catch (Exception ex) { await HandleExceptionAsync(context, ex); }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = exception switch
            {
                ArgumentException => HttpStatusCode.BadRequest, // 400
                KeyNotFoundException => HttpStatusCode.NotFound, // 404
                UnauthorizedAccessException => HttpStatusCode.Unauthorized, // 401
                DbUpdateException => HttpStatusCode.Conflict, // 409
                _ => HttpStatusCode.InternalServerError // 500
            };

            var message = GetSafeErrorMessage(exception, code);

            var result = JsonSerializer.Serialize(new
            {
                message,
                statusCode = (int)code,
                path = context.Request.Path.Value,
                traceId = context.TraceIdentifier,
                timestamp = DateTime.UtcNow
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result);
        }

        private static string GetSafeErrorMessage(Exception exception, HttpStatusCode code)
        {
            if (code == HttpStatusCode.InternalServerError)
            {
                return "An unexpected server error occurred. Please try again later.";
            }

            if (exception is DbUpdateException dbUpdateEx)
            {
                var dbMessage = dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message;
                if (dbMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                    || dbMessage.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
                    || dbMessage.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    return "Duplicate data detected. Please provide unique values and retry.";
                }

                return "Database update failed. Please verify your input data and try again.";
            }

            return exception.Message;
        }
    }
}
