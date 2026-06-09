using LinkShorter.Data;
using LinkShorter.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkShorter.Controllers;

[ApiController]
[Route("api/links")]
public class LinkController(AppDbContext context) : ControllerBase
{
    private static readonly Random Random = new();

    [HttpPost]
    public async Task<IActionResult> Shorten([FromBody] string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl) || !Uri.IsWellFormedUriString(longUrl, UriKind.Absolute))
        {
            return BadRequest("Incorrect URL format.");
        }

        string slug;
        do
        {
            slug = GenerateSlug(6);
        } 
        while (await context.Links.AnyAsync(l => l.Slug == slug));

        var link = new Link { Slug = slug, Url = longUrl };
        context.Links.Add(link);
        await context.SaveChangesAsync();

        var shortUrl = $"{Request.Scheme}://{Request.Host}/{slug}";
        return Ok(new { Slug = slug, ShortUrl = shortUrl });
    }

    [HttpGet("/{slug}")]
    public async Task<IActionResult> RedirectTo(string slug)
    {
        var link = await context.Links.FindAsync(slug);
        if (link == null)
        {
            return NotFound();
        }

        return Redirect(link.Url);
    }

    private static string GenerateSlug(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }
}