# Blazor Routing
***Have greater control over Blazor routing as well as using deeplinks with routable components.***

I wanted to be able to deep link into my application but all the useful routing related classes in `Microsoft.AspNetCore.Components.Routing` were internal.

The objectives were to:
 1. Parse the browser location string to be able to activate components within the application instead of just 'pages'. This allowed routing the side-panels, etc.
 2. Use 'catch-all' routes to allow sub-routes to be handled by specific pages. Although the catch-all route was implemented in the current [aspnetcore](https://github.com/dotnet/aspnetcore) repository, it didn't appear to be present in the releases or any of the documentation.

The microsoft implementation for the `RouteContext` mutated the state of the context instance over multiple `RouteEntry` match invocations, but only the first match appears to ever be used. The implementation has be refactored to allow matching a `RouteEntry` and either using the handler specified in the entry or just taking some action. I had implemented a generic `RouteEntry` because some of the earlier iterations involved creating a entry with a delegate callback.

## Usage

### Container Registrations

Register the `RouteContainer<Type>` which is used by the `Router`.

``` c#
public static async Task Main(string[] args)
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);

    ...


    builder.Services.AddScoped<RouteContainer<Type>, TypeRouteContainer>();

    ...
}
```

### Implementing `RoutableComponentBase`

To Make a routable component:
1. inherit from `RoutableComponentBase`
2. specify the route template string

Optionally override `OnActivate` or `OnActivateAsync` to perform an action once the route is active and `OnDeactivate` or `OnDeactivateAsync` for deactivation.


```
@* inherit from RoutableComponentBase *@
@inherit RoutableComponentBase

... razor

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

``` razor
<CascadingValue Value="Location" Name="Location">
    <Router AppAssembly="@typeof(Program).Assembly">
        ...
    </Router>
</CascadingValue>
```

The `NavigationManager` can be used to provide the `Location`

``` razor
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

The same mechanism is implemented by `RoutableComponentContainer`

``` razor
<RoutableComponentContainer>
    <Router AppAssembly="@typeof(Program).Assembly">
        ...
    </Router>
</RoutableComponentContainer>
```

### Using a Catch-All route template

When putting routable components on a page, the route must still be valid for the parent page to reach the components that it contains, so a catch-all route must be used. This is implemented in the `Router` component used in `App.razor`

The relative path parameter must be present. If you like, you can also use the relative path with the routable components by providing the cascading value `Location` with `RelativePath`. The downside to doing this is that navigation using the browser back/forward buttons does not cause the `RelativePath` value to be changed 🤦‍♂️.

`MyRoutableCompoent` can implement Routes below "/issues" like "/issues/add"

``` razor
@page "/issues"
@page "/issues/*relativePath"

<MyRoutableComponent />

@code {
    [Parameter] public string? RelativePath { get; set; }
}
```

### Routing Parameters

The parameters extracted from the route template are provided in the `OnActivate` calls.
For example, using the route template `/issues/{id}` with the path `/issues/123` would produce a dictionary with a key value of `id` and a string value `123` extracted from the path.

The route parameters can be constrained using the supported constraints. For example `/issues/{id:guid}` would not match the path `/issues/123`.

#### Supported route constraints

- bool
- bool?
- datetime
- datetime?
- decimal
- decimal?
- double
- double?
- float
- float?
- guid
- guid?
- int
- int?
- long
- long?

The `?` suffix indicates that the constrain is optional, meaning that the value can be omitted and the route will still match.