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
    public required ICheepRepository CheepRepository { get; set; }

    public List<CheepViewModel> Cheeps { get; set; } = new();

    [BindProperty]
    [StringLength(160, ErrorMessage = "Cheeps cannot be longer than 160 characters.")]
    public string? Text { get; set; } // ← now nullable!

    [BindProperty]
    public IFormFile? Upload { get; set; }

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

        // Custom validation: at least text OR image must be provided
        if (string.IsNullOrWhiteSpace(Text) && Upload == null)
        {
            ModelState.AddModelError(string.Empty, "You must either write a cheep or attach an image/GIF.");
        }

        // If we have an image, remove any "required" error on Text
        if (Upload != null && Upload.Length > 0)
        {
            ModelState.Remove("Text");
        }

        if (!ModelState.IsValid)
        {
            Cheeps = await _service.GetCheeps();
            return Page();
        }

        string? imageUrl = null;

        if (Upload != null && Upload.Length > 0)
        {
            if (!IsValidImage(Upload))
            {
                ModelState.AddModelError("Upload", "Only JPEG, PNG, and GIF files are allowed (max 5 MB).");
                Cheeps = await _service.GetCheeps();
                return Page();
            }

            imageUrl = await SaveImageAsync(Upload);
        }

        var message = new MessageDTO
        {
            Text = Text ?? string.Empty, // can be empty if image exists
            AuthorName = User.Identity!.Name!,
            TimeStamp = DateTime.UtcNow,
            ImageUrl = imageUrl
        };

        await CheepRepository.StoreCheepAsync(message);
        return RedirectToPage();
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return "/uploads/" + uniqueFileName;
    }

    private static bool IsValidImage(IFormFile file)
    {
        if (file.Length > 5 * 1024 * 1024) return false;

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant())) return false;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".gif";
    }

    // ────── Follow/Unfollow helpers ──────
    public async Task<bool> IsFollowingAsync(string followeeName)
        => User.Identity?.IsAuthenticated == true
           && User.Identity.Name is { } currentUser
           && await CheepRepository.IsFollowingAsync(currentUser, followeeName);

    public async Task<IActionResult> OnGetFollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.FollowUserAsync(user, followee);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetUnfollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.UnfollowUserAsync(user, followee);
        return RedirectToPage();
    }
}