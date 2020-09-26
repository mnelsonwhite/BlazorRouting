using System.Diagnostics;

namespace BlazorRouting
{

    [DebuggerDisplay("Handler = {Handler}, Component = {Component}, Template = {Template}")]
    public class HandlerRouteEntry<T> : RouteEntry where T : class
    {
        public HandlerRouteEntry(
            RouteTemplate template,
            T handler,
            string[] unusedRouteParameterNames
        ) : base(
            template,
            unusedRouteParameterNames)
        {
            Handler = handler;
        }

        public T? Handler { get; }
    }
}
