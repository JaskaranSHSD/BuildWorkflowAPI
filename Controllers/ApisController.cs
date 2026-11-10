using BuildWorkflowAPI.Data;
using BuildWorkflowAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuildWorkflowAPI.Controllers;

[Authorize]
public class ApisController : Controller
{
    private readonly AppDbContext _db;
    public ApisController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.ApiDefinitions.Include(a => a.Connection).ToListAsync());

    public async Task<IActionResult> Create()
    {
        ViewBag.Connections = new SelectList(await _db.Connections.ToListAsync(), "Id", "Name");
        return View(new ApiDefinition());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ApiDefinition m)
    {
        if (!ModelState.IsValid) { ViewBag.Connections = new SelectList(await _db.Connections.ToListAsync(), "Id", "Name"); return View(m); }
        _db.ApiDefinitions.Add(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var a = await _db.ApiDefinitions.Include(x => x.Connection).Include(x => x.Parameters).Include(x => x.ApiKeys).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        return View(a);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var a = await _db.ApiDefinitions.FindAsync(id);
        if (a == null) return NotFound();
        ViewBag.Connections = new SelectList(await _db.Connections.ToListAsync(), "Id", "Name");
        return View(a);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ApiDefinition m)
    {
        if (!ModelState.IsValid) { ViewBag.Connections = new SelectList(await _db.Connections.ToListAsync(), "Id", "Name"); return View(m); }
        _db.ApiDefinitions.Update(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.ApiDefinitions.FindAsync(id);
        if (a != null) { _db.ApiDefinitions.Remove(a); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddParameter(int apiId, string name, string dbType, bool isOutput, bool isRequired, string? defaultValue)
    {
        _db.ApiParameters.Add(new ApiParameter { ApiDefinitionId = apiId, Name = name, DbType = dbType, IsOutput = isOutput, IsRequired = isRequired, DefaultValue = defaultValue });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = apiId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteParameter(int id, int apiId)
    {
        var p = await _db.ApiParameters.FindAsync(id);
        if (p != null) { _db.ApiParameters.Remove(p); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Details), new { id = apiId });
    }

    [HttpPost]
    public async Task<IActionResult> AddKey(int apiId, string name)
    {
        var key = Guid.NewGuid().ToString("N");
        _db.ApiKeys.Add(new ApiKey { ApiDefinitionId = apiId, Name = name, Key = key });
        await _db.SaveChangesAsync();
        TempData["NewKey"] = key;
        return RedirectToAction(nameof(Details), new { id = apiId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteKey(int id, int apiId)
    {
        var k = await _db.ApiKeys.FindAsync(id);
        if (k != null) { _db.ApiKeys.Remove(k); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Details), new { id = apiId });
    }

    [HttpPost]
    public async Task<IActionResult> AddGlobalKey(string name)
    {
        var key = Guid.NewGuid().ToString("N");
        _db.ApiKeys.Add(new ApiKey { ApiDefinitionId = null, Name = name, Key = key });
        await _db.SaveChangesAsync();
        TempData["NewKey"] = key;
        return RedirectToAction(nameof(Index));
    }
}