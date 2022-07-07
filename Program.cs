using Microsoft.AspNetCore.Http.Extensions;
using Orleans;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
});

var app = builder.Build();

var grainFactory = app.Services.GetRequiredService<IGrainFactory>();

app.MapGet("/shorten/{*path}", async (HttpContext context, string path) =>
{
    var slug = Guid.NewGuid().GetHashCode().ToString("X");
    var shortenerGrain = grainFactory.GetGrain<IUrlShortenerGrain>(slug);
    await shortenerGrain.SetUrl(path);
    var resultBuilder = new UriBuilder(context.Request.GetEncodedUrl())
    {
        Path = $"/go/{slug}"
    };
    return Results.Text(resultBuilder.Uri.ToString());
});

app.MapGet("/go/{shortenedRouteSegment}", async (string shortenedRouteSegment) =>
{
    var shortenerGrain = grainFactory.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
    var url = await shortenerGrain.GetUrl();
    return string.IsNullOrWhiteSpace(url) ? Results.NotFound() : Results.Redirect(url);
});

app.MapGet("/count/{shortenedRouteSegment}", async (string shortenedRouteSegment) =>
{
    var shortenerGrain = grainFactory.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
    var count = await shortenerGrain.GetCount();
    return Results.Text(count.ToString());    
});

app.Run();

public interface IUrlShortenerGrain : IGrainWithStringKey
{
  Task SetUrl(string url);
  Task<string> GetUrl();
  Task<int> GetCount();
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
  private string _url = "";
  private int _counter = 0;

  public Task SetUrl(string url)
  {
    _url = url;
    return Task.CompletedTask;
  }

  public Task<string> GetUrl()
  {
    _counter += 1;
    return Task.FromResult(_url);
  }

  public Task<int> GetCount()
  {
    return Task.FromResult(_counter);
  }
}

