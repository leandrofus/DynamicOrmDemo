using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json.Nodes;
using System.IO;
using DynamicOrmLib;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace DynamicOrmDemo.Controllers;

public class AdminController : Controller
{
  private readonly DynamicContext _ctx;
  private readonly string _uploadsPath;
  private readonly string _adminPassword;

  public AdminController(DynamicContext ctx, IConfiguration config)
  {
    _ctx = ctx;
    _uploadsPath = config["UploadsPath"] ?? "wwwroot/uploads";
    _adminPassword = config["AdminPassword"] ?? "admin123";
  }

  public IActionResult Login(string? returnUrl)
  {
    ViewData["ReturnUrl"] = returnUrl ?? "/admin";
    return View();
  }

  [HttpPost]
  public async Task<IActionResult> LoginPost(string password, string returnUrl)
  {
    if (password == _adminPassword)
    {
      var claims = new[] { new Claim(ClaimTypes.Name, "admin") };
      var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
      var principal = new ClaimsPrincipal(identity);
      await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
      return LocalRedirect(returnUrl ?? "/admin");
    }
    ModelState.AddModelError("", "Invalid password");
    return View("Login");
  }

  public async Task<IActionResult> Logout()
  {
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Index", "Home");
  }

  [Authorize]
  public IActionResult Index()
  {
    var posts = _ctx.GetRecords("post", new QueryOptions { OrderBy = "createdAt", OrderDesc = true }).ToList();
    return View(posts);
  }

  [Authorize]
  public IActionResult Create()
  {
    return View();
  }

  [Authorize]
  [HttpPost]
  public IActionResult CreatePost(IFormFile? coverImage, string title, string slug, string body)
  {
    var data = new JsonObject { ["title"] = title, ["slug"] = slug, ["body"] = body, ["createdAt"] = DateTime.UtcNow.ToString("o") };
    if (coverImage != null && coverImage.Length > 0)
    {
      var uploadsAbs = Path.Combine(Directory.GetCurrentDirectory(), _uploadsPath.Replace('/', Path.DirectorySeparatorChar));
      var fn = Path.GetFileName(coverImage.FileName);
      var uniq = Guid.NewGuid().ToString("N") + "_" + fn;
      var dest = Path.Combine(uploadsAbs, uniq);
      using (var fs = System.IO.File.Create(dest)) { coverImage.CopyTo(fs); }
      var url = "/uploads/" + uniq;
      data["coverImageUrl"] = url;
    }
    var rec = _ctx.CreateRecord("post", data);
    return RedirectToAction("Index");
  }

  [Authorize]
  public IActionResult Edit(string id)
  {
    var rec = _ctx.GetById(id);
    if (rec == null) return NotFound();
    return View(rec);
  }

  [Authorize]
  [HttpPost]
  public IActionResult EditPost(string id, IFormFile? coverImage, string title, string slug, string body)
  {
    var rec = _ctx.GetById(id);
    if (rec == null) return NotFound();
    if (coverImage != null && coverImage.Length > 0)
    {
      var uploadsAbs = Path.Combine(Directory.GetCurrentDirectory(), _uploadsPath.Replace('/', Path.DirectorySeparatorChar));
      var fn = Path.GetFileName(coverImage.FileName);
      var uniq = Guid.NewGuid().ToString("N") + "_" + fn;
      var dest = Path.Combine(uploadsAbs, uniq);
      using (var fs = System.IO.File.Create(dest)) { coverImage.CopyTo(fs); }
      var url = "/uploads/" + uniq;
      rec.Data["coverImageUrl"] = url;
    }
    rec.Data["title"] = title;
    rec.Data["slug"] = slug;
    rec.Data["body"] = body;
    _ctx.UpdateRecord(rec.Id, rec.Data);
    return RedirectToAction("Index");
  }

  [Authorize]
  [HttpPost]
  public IActionResult Delete(string id)
  {
    try { _ctx.DeleteRecord(id); } catch { }
    return RedirectToAction("Index");
  }
}
