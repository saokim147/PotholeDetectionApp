
using MapApi.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Map Tile API",
        Version = "v1",
        Description = "An API to fetch map tiles in .pbf format",
    });
});

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSingleton(
    sp =>
    {
        var path = "./Static/osm.mbtiles";
        return new MBTileProvider(path);
    }
);



var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Static")),
    RequestPath = "/static"
});
app.MapGet("/", () => "Map Checkpoint");
app.MapGet("/tiles/{z}/{x}/{y}.pbf", (int z, int x, int y, MBTileProvider provider, HttpContext context) =>
{
    var data = provider.GetTiles(z, x, y);
    if (data == null)
    {
        return Results.NotFound("Tile not found");
    }
    context.Response.Headers.ContentEncoding = "gzip";
    return Results.File(data, "application/x-protobuf",$"{z}.pbf");
});
app.MapGet("/Map/{fontstack}/{range:regex(^[[\\d]]+-[[\\d]]+$)}.pbf", async (HttpContext context, string fontstack, string range) =>
{
    fontstack = Uri.UnescapeDataString(fontstack);
    var rootPath = Path.Combine(Directory.GetCurrentDirectory(),"Static","font");
    var fileName = Path.Combine(rootPath, fontstack, $"{range}.pbf");

    if (!File.Exists(fileName))
    {
        return Results.NotFound("Font not found");
    }

    var data = await File.ReadAllBytesAsync(fileName);

    context.Response.Headers.LastModified= DateTime.UtcNow.ToString("R"); // RFC1123 format

    return Results.File(data, "application/x-protobuf", $"{fontstack}-{range}.pbf");
});



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Map Tile API v1");
    });
}
if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.Run();



