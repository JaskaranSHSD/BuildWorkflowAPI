using System.Net.Http.Json;
using System.Text.Json;
using BuildWorkflowAPI.Data;
using BuildWorkflowAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuildWorkflowAPI.Controllers;

public class SsoController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public SsoController(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        _db = db;
        _httpFactory = httpFactory;
        _cfg = cfg;
    }

    [HttpGet]
    public IActionResult Login(string? token = null)
    {
        if (string.IsNullOrWhiteSpace(token))
            return View(); // show token input form

        return RedirectToAction(nameof(Callback), new { token });
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Callback(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Missing SSO token." });

        var validationUrl = _cfg["Sso:ValidationUrl"];
        if (string.IsNullOrWhiteSpace(validationUrl))
            return StatusCode(503, new { error = "SSO validation URL is not configured." });

        try
        {
            var client = _httpFactory.CreateClient();
            var url = $"{validationUrl.TrimEnd('?')}?token={Uri.EscapeDataString(token)}";
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return Unauthorized(new { error = "SSO token validation failed." });

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            string? username = TryGetString(root, "username") ?? TryGetString(root, "user_name") ?? TryGetString(root, "sub");
            string? email = TryGetString(root, "email");
            bool exists = root.TryGetProperty("exists", out var existsEl) && existsEl.ValueKind == JsonValueKind.True;

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { error = "SSO response did not include a username." });

            // Look up existing user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
            if (user is null)
            {
                // Create on first SSO login
                var hashUser = new AppUser { Username = username, Email = string.IsNullOrWhiteSpace(email) ? $"{username}@sso.local" : email! };
                user = new AppUser
                {
                    Username = username,
                    Email = hashUser.Email,
                    PasswordHash = _hasher.HashPassword(hashUser, Guid.NewGuid().ToString("N"))
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);
            }
            else if (!string.IsNullOrWhiteSpace(email) && user.Email != email)
            {
                // Keep email in sync with SSO provider
                user.Email = email!;
                await _db.SaveChangesAsync(ct);
            }

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.Name, user.Username),
                new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }
        catch
        {
            return StatusCode(500, new { error = "SSO login failed." });
        }
    }

    private static string? TryGetString(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }
}