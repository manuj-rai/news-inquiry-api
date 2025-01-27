using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyApi

{
    public class AllowSpecificOriginFilter : IActionFilter
    {
        private readonly string[] _allowedOrigins;

        public AllowSpecificOriginFilter(string allowedOrigins)
        {
            _allowedOrigins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var requestOrigin = context.HttpContext.Request.Headers["Origin"].ToString();

            // Validate the Origin header
            if (!string.IsNullOrEmpty(requestOrigin) && !_allowedOrigins.Contains(requestOrigin))
            {
                context.Result = new ForbidResult(); // Deny access
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No operation after the action executes
        }
    }
}
