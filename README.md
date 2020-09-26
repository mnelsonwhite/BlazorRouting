# Blazor Routing
***Have greater control over Blazor routing as well as using deeplinks with routable components.***

I wanted to be able to deep link into my application but all the useful routing related classes in `Microsoft.AspNetCore.Components.Routing` were internal.

The objectives were to:
 1. Parse the browser location string to be able to activate components within the application instead of just 'pages'. This allowed routing the side-panels, etc.
 2. Use 'catch-all' routes to allow sub-routes to be handled by specific pages. Although the catch-all route was implemented in the current [aspnetcore](https://github.com/dotnet/aspnetcore) repository, it didn't appear to be present in the releases or any of the documentation.

The microsoft implementation for the `RouteContext` mutated the state of the context instance over multiple `RouteEntry` match invocations, but only the first match appears to ever be used. The implementation has be refactored to allow matching a `RouteEntry` and either using the handler specified in the entry or just taking some action. I had implemented a generic `RouteEntry` because some of the earlier iterations involved creating a entry with a delegate callback.

## Usage
### Implementing `RoutableComponentBase`

To Make a routable component:
1. inherit from `RoutableComponentBase`
2. specify the route template string

Optionally override `OnActivate` or `OnActivateAsync` to perform an action once the route is active and `OnDeactivate` or `OnDeactivateAsync` for deactivation.


```
@* inherit from RoutableComponentBase *@
@inherit RoutableComponentBase

...

@code {
  // Specify route template string
  protected override string Route => "/issues/add";

  protected override Task OnActivateAsync(Dictionary<string, object?> parameters)
  {
    ...
  }

  protected override void OnActivate(Dictionary<string, object?> parameters)
  {
    ...
  }

  protected override Task OnDeactivateAsync()
  {
    ...
  }

  protected override void OnDeactivate()
  {
    ...
  }
}
```

### Providing the `Location`

Reference the library and replace the `Router` used in `App.Razor`. The same syntax is used. The `RoutedComponentBase` expects a cascading value called `Location`. The can be provided many different ways, but if the whole application has access to this cascading value. The `Location` is the current browser location.

```
<CascadingValue Value="Location" Name="Location">
    <Router AppAssembly="@typeof(Program).Assembly">
        ...
    </Router>
</CascadingValue>
```

The `NavigationManager` can be used to provide the `Location`

```
@inject NavigationManager NavigationManager

@code {
    private string? Location { get; set; }

    protected override void OnInitialized()
    {
        Location = NavigationManager.Uri; // Get initial location
        NavigationManager.LocationChanged += LocationChanged; // Get location changes
    }

    private void LocationChanged(
        object sender,
        Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        Location = args.Location;
        StateHasChanged();
    }
}
```

This can be easily converted to a component.

```
@inject NavigationManager NavigationManager

<CascadingValue Value="Location" Name="Location">
    @ChildContent
</CascadingValue>

@code {
    private string? Location { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        Location = NavigationManager.Uri;
        NavigationManager.LocationChanged += LocationChanged;
    }

    private void LocationChanged(
        object sender,
        Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        Location = args.Location;
        StateHasChanged();
    }
}
```

