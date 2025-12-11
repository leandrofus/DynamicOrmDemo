using System.Text.Json.Nodes;
using DynamicOrmLib;
using DynamicOrmLib.Adapters.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
// Bind server to fixed port for integration testing
builder.WebHost.UseUrls("http://127.0.0.1:5011");

// Load config values
var adminPassword = builder.Configuration["AdminPassword"] ?? "admin123";
var uploadsPath = builder.Configuration["UploadsPath"] ?? "wwwroot/uploads";

// MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
  options.LoginPath = "/admin/login";
});

// SQLite demo DB
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "demo.db");
var adapter = new SqliteStoreAdapter($"Data Source={dbPath}", tablePrefix: string.Empty);
builder.Services.AddSingleton(adapter);
builder.Services.AddSingleton(new ModuleManager());
builder.Services.AddSingleton<DynamicContext>(sp => new DynamicContext(sp.GetRequiredService<SqliteStoreAdapter>()));

var app = builder.Build();
Console.WriteLine("Starting server: http://127.0.0.1:5011");

// Ensure uploads directory exists
var uploadsAbsolute = Path.Combine(Directory.GetCurrentDirectory(), uploadsPath.Replace('/', Path.DirectorySeparatorChar));
Directory.CreateDirectory(uploadsAbsolute);

// Enable static files and default files (wwwroot)
// Serve static files including uploads folder
app.UseStaticFiles();
// Additionally ensure uploads folder is served (already under wwwroot/uploads by default)

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Load manifest (prefer demo blog manifest)
var mm = app.Services.GetRequiredService<ModuleManager>();
// DynamicContext is registered as a singleton earlier
var ctx = app.Services.GetRequiredService<DynamicContext>();

void readDirectory(string path)
{
  var dir = new DirectoryInfo(path);
  if (!dir.Exists)
  {
    Console.WriteLine($"Manifest directory does not exist: {path}");
    return;
  }
  // Find all manifest files ending with -manifest.json and register them
  foreach (var file in dir.EnumerateFiles("*-manifest.json"))
  {
    try
    {
      var abs = Path.GetFullPath(file.FullName);
      Console.WriteLine("Found manifest file: " + abs);
      if (!File.Exists(abs)) { Console.WriteLine("File disappeared: " + abs); continue; }
      var manifestJson = File.ReadAllText(abs);
      var manifest = ManifestLoader.LoadFromJson(manifestJson);
      mm.Install(new[] { manifest }, adapter);
      ctx.RegisterManifest(manifest);
      Console.WriteLine($"Installed and registered manifest: {manifest.Module?.Name}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to load manifest {file.FullName}: {ex.Message}");
    }
  }
}

string manifestRoot = Path.GetFullPath(AppContext.BaseDirectory + "/specs/");
readDirectory(manifestRoot);
// Seed a sample post if none exist
try
{
  var existing = ctx.GetRecords("post");
  if (!existing.Any())
  {
    var data = new JsonObject { ["title"] = "Welcome to DynamicOrm Blog", ["slug"] = "welcome", ["body"] = "This is a sample post created on first run.", ["createdAt"] = DateTime.UtcNow.ToString("o") };
    ctx.CreateRecord("post", data);
  }
}
catch { }

// Enable simple default route to Home/Index
app.MapControllerRoute(
  name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
