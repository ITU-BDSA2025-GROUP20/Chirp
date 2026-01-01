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

    public int PageNumber { get; set; }
    public bool HasNextPage { get; set; }

    [BindProperty]
    [StringLength(160)]
    public string? Text { get; set; }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public UserTimelineModel(CheepService service, ICheepRepository repo)
    {
        _service = service;
        CheepRepository = repo;
    }

    public async Task<IActionResult> OnGetAsync(string author, int p = 1)
    {
        Author = author;
        PageNumber = p < 1 ? 1 : p;

        Cheeps = User.Identity?.IsAuthenticated == true && User.Identity.Name == author
            ? await _service.GetPrivateTimeline(author, PageNumber)
            : await _service.GetCheepsFromAuthor(author, PageNumber);

        HasNextPage = Cheeps.Count == 32;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? author = null)
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Forbid();
        author ??= User.Identity!.Name;

        if (string.IsNullOrWhiteSpace(Text) && Upload == null)
        {
            ModelState.AddModelError(string.Empty, "You must write something or upload an image/GIF.");
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
        return RedirectToPage(new { author, p = 1 });
    }

    private async Task LoadCheepsAsync(string? author)
    {
        Cheeps = User.Identity?.Name == author
            ? await _service.GetPrivateTimeline(author!, PageNumber)
            : await _service.GetCheepsFromAuthor(author!, PageNumber);
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
        return RedirectToPage(new { author = RouteData.Values["author"], p = PageNumber });
    }

    public async Task<IActionResult> OnGetUnfollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.UnfollowUserAsync(user, followee);
        return RedirectToPage(new { author = RouteData.Values["author"], p = PageNumber });
    }
}
