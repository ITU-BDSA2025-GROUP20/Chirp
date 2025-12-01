// src/Chirp.Web/Pages/UserTimeline.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
using Core;
using System.ComponentModel.DataAnnotations;

namespace Web.Pages;

public class UserTimelineModel : PageModel
{
    private readonly CheepService _service;
    public required ICheepRepository CheepRepository { get; set; }

    public List<CheepViewModel> Cheeps { get; set; } = new();
    public string Author { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheeps cannot be longer than 160 characters.")]
    [Required(ErrorMessage = "You must write something to cheep!")]
    public string Text { get; set; } = string.Empty;

    public UserTimelineModel(CheepService service, ICheepRepository cheepRepository)
    {
        _service = service;
        CheepRepository = cheepRepository;
    }

    public async Task<IActionResult> OnGetAsync(string author)
    {
        Author = author;

        if (User.Identity?.IsAuthenticated == true && User.Identity.Name == author)
        {
            // My own timeline → show private timeline (me + people I follow)
            Cheeps = await _service.GetPrivateTimeline(author);
        }
        else
        {
            // Someone else's timeline → only their cheeps
            Cheeps = await _service.GetCheepsFromAuthor(author);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string author)
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Forbid();

        if (!ModelState.IsValid)
        {
            Cheeps = User.Identity?.Name == author
                ? await _service.GetPrivateTimeline(author)
                : await _service.GetCheepsFromAuthor(author);
            return Page();
        }

        var message = new MessageDTO
        {
            Text = Text,
            AuthorName = User.Identity!.Name!,
            TimeStamp = DateTime.UtcNow
        };

        await CheepRepository.StoreCheepAsync(message);
        return RedirectToPage(new { author });
    }

    // ──────────────────────────────────────────────────────────────
    // Safe helpers used from the Razor view
    // ──────────────────────────────────────────────────────────────
    public async Task<bool> IsFollowingAsync(string followeeName)
    {
        if (User.Identity?.IsAuthenticated != true || User.Identity.Name is not { } currentUser)
            return false;

        return await CheepRepository.IsFollowingAsync(currentUser, followeeName);
    }

    public async Task<IActionResult> OnGetFollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } currentUser) return Forbid();
        await CheepRepository.FollowUserAsync(currentUser, followee);
        return RedirectToPage(new { author = RouteData.Values["author"] ?? "" });
    }

    public async Task<IActionResult> OnGetUnfollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } currentUser) return Forbid();
        await CheepRepository.UnfollowUserAsync(currentUser, followee);
        return RedirectToPage(new { author = RouteData.Values["author"] ?? "" });
    }
}