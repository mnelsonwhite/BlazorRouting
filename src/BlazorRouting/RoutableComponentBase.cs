using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using BlazorRouting;
using System;

namespace BlazorRouting
{
    public abstract class RoutableComponentBase : ComponentBase
    {
        [CascadingParameter(Name = "Location")] string? Location { get; set; }

        protected bool Active { get; private set; }
        protected abstract string? Route { get; }

        private RouteEntry? RouteEntry { get; set; }

        public RoutableComponentBase()
        {
            if (Route != null)
            {
                // Parse route template
                var routeTemplate = TemplateParser.ParseTemplate(Route);
                RouteEntry = new RouteEntry(
                    routeTemplate,
                    Array.Empty<string>()
                );
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (RouteEntry == null || Location == null) return;

            var path = new Uri(Location).PathAndQuery;

            if (Active = RouteEntry.TryMatch(path, out var parameters))
            {
                await OnActivateAsync(parameters);
                OnActivate(parameters);
            }
            else
            {
                await OnDeactivateAsync();
                OnDeactivate();
            }
        }

        protected virtual Task OnActivateAsync(Dictionary<string, object?> parameters) => Task.CompletedTask;

        protected virtual void OnActivate(Dictionary<string, object?> parameters) { }

        protected virtual Task OnDeactivateAsync() => Task.CompletedTask;

        protected virtual void OnDeactivate() { }
    }
}

