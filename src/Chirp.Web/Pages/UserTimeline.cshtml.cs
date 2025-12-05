// src/Chirp.Web/Pages/UserTimeline.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
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
    public string? Text { get; set; } // ← now nullable

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public UserTimelineModel(CheepService service, ICheepRepository repo)
    {
        _service = service;
        CheepRepository = repo;
    }

    public async Task<IActionResult> OnGetAsync(string author)
    {
        Author = author;

        Cheeps = User.Identity?.IsAuthenticated == true && User.Identity.Name == author
            ? await _service.GetPrivateTimeline(author)
            : await _service.GetCheepsFromAuthor(author);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? author = null)
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Forbid();

        // At least one of text or image required
        if (string.IsNullOrWhiteSpace(Text) && Upload == null)
        {
            ModelState.AddModelError(string.Empty, "You must write something or upload an image/GIF.");
        }

        if (Upload != null && Upload.Length > 0)
        {
            ModelState.Remove("Text"); // allow empty text if image present
        }

        if (!ModelState.IsValid)
        {
            await LoadCheepsAsync(author);
            return Page();
        }

        string? imageUrl = null;
        if (Upload != null && Upload.Length > 0)
        {
            if (!IsValidImage(Upload))
            {
                ModelState.AddModelError("Upload", "Only JPEG, PNG, GIF allowed (max 5MB).");
                await LoadCheepsAsync(author);
                return Page();
            }
            imageUrl = await SaveImageAsync(Upload);
        }

        var message = new MessageDTO
        {
            Text = Text ?? string.Empty,
            AuthorName = User.Identity!.Name!,
            TimeStamp = DateTime.UtcNow,
            ImageUrl = imageUrl
        };

        await CheepRepository.StoreCheepAsync(message);
        return RedirectToPage(new { author });
    }

    private async Task LoadCheepsAsync(string? author)
    {
        Cheeps = author == null
            ? await _service.GetCheeps()
            : User.Identity?.Name == author
                ? await _service.GetPrivateTimeline(author)
                : await _service.GetCheepsFromAuthor(author);
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        Directory.CreateDirectory(folder);

        var fileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
        var path = Path.Combine(folder, fileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return "/uploads/" + fileName;
    }

    private static bool IsValidImage(IFormFile file)
    {
        if (file.Length > 5 * 1024 * 1024) return false;
        var type = file.ContentType.ToLowerInvariant();
        return type is "image/jpeg" or "image/jpg" or "image/png" or "image/gif";
    }

    // Follow helpers
    public async Task<bool> IsFollowingAsync(string name)
        => User.Identity?.IsAuthenticated == true
           && await CheepRepository.IsFollowingAsync(User.Identity.Name!, name);

    public async Task<IActionResult> OnGetFollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.FollowUserAsync(user, followee);
        return RedirectToPage(new { author = RouteData.Values["author"] ?? "" });
    }

    public async Task<IActionResult> OnGetUnfollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.UnfollowUserAsync(user, followee);
        return RedirectToPage(new { author = RouteData.Values["author"] ?? "" });
    }
}