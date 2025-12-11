using System.Text.Json.Nodes;
using System.Linq;
using DynamicOrmLib;
using DynamicOrmLib.Adapters.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;
using Microsoft.Extensions.FileProviders;

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
var candidatePaths = new[] {
  "specs/blog-manifest.json",
  "specs/blog-manifest.json",
  "../DynamicOrmLib/specs/crm-manifest.json",
  "../DynamicOrmLib/specs/crm-manifest.json",
  "DynamicOrmLib/specs/crm-manifest.json"
};
string? manifestPath = null;
foreach (var p in candidatePaths)
{
  var abs = Path.GetFullPath(p);
  if (File.Exists(abs)) { manifestPath = abs; break; }
}
if (manifestPath == null) throw new FileNotFoundException("Manifest file not found in expected locations");
var manifestJson = File.ReadAllText(manifestPath);
var manifest = ManifestLoader.LoadFromJson(manifestJson);

// Install manifest into adapter and register with context
var mm = app.Services.GetRequiredService<ModuleManager>();
mm.Install(new[] { manifest }, adapter);

var ctx = app.Services.CreateScope().ServiceProvider.GetRequiredService<DynamicContext>();
ctx.RegisterManifest(manifest);

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
