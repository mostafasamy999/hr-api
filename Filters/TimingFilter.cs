using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HrApi.Filters;

// ACTION FILTER: code that runs BEFORE and AFTER a controller action.
// Filters are one of the main reasons people choose controllers: put [ServiceFilter(typeof(TimingFilter))]
// on a controller (or register it globally) and every action gets this behavior.
// (Minimal APIs have a similar idea called "endpoint filters".)
public class TimingFilter(ILogger<TimingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var action = context.ActionDescriptor.DisplayName;

        var executed = await next();   // <- the action runs here

        context.HttpContext.Response.Headers["X-Elapsed-Ms"] = sw.ElapsedMilliseconds.ToString();
        logger.LogInformation("⏱ {Action} took {Ms} ms", action, sw.ElapsedMilliseconds);
    }
}
