using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using BlazorRouting;
using System;

namespace TechDebtRadar.App.Client.Shared
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
                    new string[0]
                );
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (RouteEntry == null || Location == null) return;

            var path = new Uri(Location).PathAndQuery;
            var match = RouteEntry.Match(path);

            if (match.IsSuccess)
            {
                await OnActivateAsync(match.Value);
                OnActivate(match.Value);
            }
            else
            {
                await OnDeactivateAsync();
                OnDeactivate();
            }

            Active = match.IsSuccess;
        }

        protected virtual Task OnActivateAsync(Dictionary<string, object?> parameters)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnActivate(Dictionary<string, object?> parameters)
        {
        }

        protected virtual Task OnDeactivateAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnDeactivate()
        {
        }
    }
}

