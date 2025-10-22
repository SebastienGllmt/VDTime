using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

public static class RestAdaptor
{
  public static void createRestAdaptor(StateManager stateManager, int port) {
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls($"http://localhost:{port}");
    var app = builder.Build();

    app.MapGet("/healthz", () => Results.Ok("ok"));
    app.MapGet("/get_desktops", () =>
    {
      return Results.Json(stateManager.getDesktops(), new JsonSerializerOptions { WriteIndented = false });
    });
    app.MapGet("/curr_desktop", () =>
    {
      
      return Results.Json(stateManager.getCurrentDesktop(), new JsonSerializerOptions { WriteIndented = false });
    });
    app.MapGet("/time_on", (HttpRequest req) =>
    {
      var name = req.Query["name"].ToString();
      var guid = req.Query["guid"].ToString();
      return Results.Json(stateManager.getTimeOn(name, guid));
    });
    app.MapGet("/time_all", () =>
    {
      return Results.Json(stateManager.getTimeAll(), new JsonSerializerOptions { WriteIndented = false });
    });
    app.MapPost("/reset", () =>
    {
      stateManager.reset();
      return Results.Ok();
    });
    Console.WriteLine($"vdtime-core listening on http://localhost:{port}");
    app.Run();
  }
}