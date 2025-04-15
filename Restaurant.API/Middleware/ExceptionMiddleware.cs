using System.Net;
using Restaurant.Application.Exceptions;

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

                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ErrorResponse
                    {
                        Title = "Invalid or expired access token",
                        Status = StatusCodes.Status401Unauthorized,
                        Type = "Unauthorized",
                    });
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            ErrorResponse problem;

            switch (exception)
            {
                case UnauthorizedException unauthorizedException:
                    logger.LogWarning(
                        unauthorizedException,
                        "Unauthorized access attempt: {Path}",
                        context.Request.Path);
                    statusCode = HttpStatusCode.Unauthorized;
                    problem = new ErrorResponse
                    {
                        Title = unauthorizedException.Message,
                        Status = (int)statusCode,
                        Detail = unauthorizedException.InnerException?.Message,
                        Type = nameof(UnauthorizedException),
                    };
                    break;
                case BadRequestException badRequestException:
                    logger.LogWarning(exception, "Validation failed: {Path}", context.Request.Path);
                    statusCode = HttpStatusCode.BadRequest;
                    problem = new ErrorResponse
                    {
                        Title = badRequestException.Message,
                        Status = (int)statusCode,
                        Detail = badRequestException.InnerException?.Message,
                        Type = nameof(BadRequestException),
                        Errors = badRequestException.ValidationErrors ?? new Dictionary<string, string[]>(),
                    };
                    break;
                case NotFoundException notFound:
                    logger.LogWarning(notFound, "Resource not found: {Message}", notFound.Message);
                    statusCode = HttpStatusCode.NotFound;
                    problem = new ErrorResponse
                    {
                        Title = notFound.Message,
                        Status = (int)statusCode,
                        Detail = notFound.InnerException?.Message,
                        Type = nameof(NotFoundException),
                    };
                    break;
                default:
                    logger.LogError(exception, "Unhandled exception occurred");
                    problem = new ErrorResponse
                    {
                        Title = exception.Message,
                        Status = (int)statusCode,
                        Detail = exception.StackTrace,
                        Type = nameof(HttpStatusCode.InternalServerError),
                    };
                    break;
            }

            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
