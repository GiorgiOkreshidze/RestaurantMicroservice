using Microsoft.AspNetCore.Mvc;

namespace Restaurant.API.Middleware
{
    public class ErrorResponse : ProblemDetails
    {
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }
}
