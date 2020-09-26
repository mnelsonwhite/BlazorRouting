using System.Collections.Generic;
using System.Linq;


namespace BlazorRouting
{
    public class RouteContainer<T> where T : class
    {
        private readonly SortedSet<HandlerRouteEntry<T>> _routeEntries;

        public RouteContainer()
        {
            _routeEntries = new SortedSet<HandlerRouteEntry<T>>();
        }

        public HandlerRouteEntry<T> Register(
            string template,
            T hander,
            params string[] unusedRouteParameterNames)
        {
            var routeTemplate = TemplateParser.ParseTemplate(template);
            var routeEntry = new HandlerRouteEntry<T>(routeTemplate, hander, unusedRouteParameterNames);
            _routeEntries.Add(routeEntry);

            return routeEntry;
        }

        public void Merge(HandlerRouteEntry<T>[] routeEntries)
        {
            foreach(var routeEntry in routeEntries)
            {
                _routeEntries.Add(routeEntry);
            }
        }

        public Result<(HandlerRouteEntry<T>? RouteEntry, Dictionary<string, object?> Parameters)> TryMatch(string path)
        {
            var result = _routeEntries
                .Select(r => new { routeEntry = r, parameters = r.Match(path) })
                .FirstOrDefault(r => r.parameters.IsSuccess);

            return result == null
                ? Result<(HandlerRouteEntry<T>?, Dictionary<string, object?>)>.Failed()
                : Result<(HandlerRouteEntry<T>?, Dictionary<string, object?>)>.Success((result.routeEntry, result.parameters.Value));
        }

        public IEnumerable<HandlerRouteEntry<T>> Entries => _routeEntries;
    }
}
