// src/Chirp.Web/Pages/Public.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
using Core;
using System.ComponentModel.DataAnnotations;

namespace Web.Pages;

public class PublicModel : PageModel
{
    private readonly CheepService _service;
    public required ICheepRepository CheepRepository { get; set; } // DI will fill it

    public List<CheepViewModel> Cheeps { get; set; } = new();

    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheeps cannot be longer than 160 characters.")]
    [Required(ErrorMessage = "You must write something to cheep!")]
    public string Text { get; set; } = string.Empty;

    public PublicModel(CheepService service, ICheepRepository cheepRepository)
    {
        _service = service;
        CheepRepository = cheepRepository;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Cheeps = await _service.GetCheeps();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Forbid();

        if (!ModelState.IsValid)
        {
            Cheeps = await _service.GetCheeps();
            return Page();
        }

        var message = new MessageDTO
        {
            Text = Text,
            AuthorName = User.Identity!.Name!, // safe – we checked IsAuthenticated
            TimeStamp = DateTime.UtcNow
        };

        await CheepRepository.StoreCheepAsync(message);
        return RedirectToPage();
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
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetUnfollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } currentUser) return Forbid();
        await CheepRepository.UnfollowUserAsync(currentUser, followee);
        return RedirectToPage();
    }
}