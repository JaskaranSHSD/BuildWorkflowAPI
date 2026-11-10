using BuildWorkflowAPI.Data;
using BuildWorkflowAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildWorkflowAPI.Controllers;

[Authorize]
public class ConnectionsController : Controller
{
    private readonly AppDbContext _db;
    public ConnectionsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Connections.ToListAsync());

    public IActionResult Create() => View(new ApiConnection());

    [HttpPost]
    public async Task<IActionResult> Create(ApiConnection m)
    {
        if (!ModelState.IsValid) return View(m);
        _db.Connections.Add(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Connections.FindAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ApiConnection m)
    {
        if (!ModelState.IsValid) return View(m);
        _db.Connections.Update(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Connections.FindAsync(id);
        if (c != null) { _db.Connections.Remove(c); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}