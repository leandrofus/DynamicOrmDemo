using Microsoft.AspNetCore.Mvc;
using DynamicOrmLib;
using System.Text.Json.Nodes;

namespace DynamicOrmDemo.Controllers;

public class HomeController : Controller
{
  private readonly DynamicContext _ctx;

  public HomeController(DynamicContext ctx)
  {
    _ctx = ctx;
  }

  public IActionResult Index()
  {
    var opts = new QueryOptions { OrderBy = "createdAt", OrderDesc = true };
    var posts = _ctx.GetRecords("post", opts).ToList();
    return View(posts);
  }

  public IActionResult Details(string id)
  {
    var rec = _ctx.GetById(id);
    if (rec == null) return NotFound();
    return View(rec);
  }
}
