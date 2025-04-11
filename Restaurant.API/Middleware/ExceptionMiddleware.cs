using Amazon.Runtime.Internal;
using Restaurant.Application.Exceptions;
using System.Net;

namespace Restaurant.API.Middleware
{
    public class ExceptionMiddleware(
         RequestDelegate next,
         ILogger<ExceptionMiddleware> logger
        )
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            ErrorResponse problem = new();

            switch (exception)
            {
                case UnauthorizedException unauthorizedException:
                    statusCode = HttpStatusCode.Unauthorized;
                    problem = new ErrorResponse
                    {
                        Title = unauthorizedException.Message,
                        Status = (int)statusCode,
                        Detail = unauthorizedException.InnerException?.Message,
                        Type = nameof(UnauthorizedException),
                    };
                    logger.LogWarning(
                        unauthorizedException,
                        "Unauthorized access attempt: {Path}",
                        context.Request.Path);
                    break;

                case BadRequestException badRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    problem = new ErrorResponse
                    {
                        Title = badRequestException.Message,
                        Status = (int)statusCode,
                        Detail = badRequestException.InnerException?.Message,
                        Type = nameof(BadRequestException),
                        Errors = badRequestException.ValidationErrors ?? new Dictionary<string, string[]>(),
                    };
                    logger.LogWarning(exception, "Resource not found: {Path}", context.Request.Path);
                    break;

                case NotFoundException notFound:
                    statusCode = HttpStatusCode.NotFound;
                    problem = new ErrorResponse
                    {
                        Title = notFound.Message,
                        Status = (int)statusCode,
                        Detail = notFound.InnerException?.Message,
                        Type = nameof(NotFoundException),
                    };
                    logger.LogWarning(notFound, "Validation failed: {Message}", notFound.Message);
                    break;

                default:
                    problem = new ErrorResponse
                    {
                        Title = exception.Message,
                        Status = (int)statusCode,
                        Detail = exception.StackTrace,
                        Type = nameof(HttpStatusCode.InternalServerError),
                    };

                    logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
