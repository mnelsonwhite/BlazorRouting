using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace TechDebtRadar.App.Client.Shared
{
    public class RoutableComponentContainer : ComponentBase
    {
        private string? Location { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Inject] public NavigationManager? NavigationManager { get; set; }

        protected override void OnInitialized()
        {
            Location = NavigationManager!.Uri;
            NavigationManager!.LocationChanged += LocationChanged;
        }

        private void LocationChanged(
            object sender,
            LocationChangedEventArgs args)
        {
            Location = args.Location;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<string?>>(0);
            builder.AddAttribute(1, "Name", "Location");
            builder.AddAttribute(2, "Value", Location);
            builder.AddAttribute(3, "ChildContent", ChildContent);
            builder.CloseComponent();

            base.BuildRenderTree(builder);
        }
    }
}

