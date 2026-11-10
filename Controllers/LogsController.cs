using BuildWorkflowAPI.Data;
using BuildWorkflowAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildWorkflowAPI.Controllers;

[Authorize]
public class LogsController : Controller
{
    private readonly AppDbContext _db;
    public LogsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var logs = await _db.ApiCallLogs
            .Include(l => l.ApiDefinition)
            .Include(l => l.ApiKey)
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToListAsync();
        return View(logs);
    }
}