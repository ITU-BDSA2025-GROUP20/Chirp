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

    public int PageNumber { get; set; }
    public bool HasNextPage { get; set; }

    [BindProperty]
    [StringLength(160)]
    public string? Text { get; set; }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public PublicModel(CheepService service, ICheepRepository repo)
    {
        _service = service;
        CheepRepository = repo;
    }

    public async Task<IActionResult> OnGetAsync(int p = 1)
    {
        PageNumber = p < 1 ? 1 : p;
        Cheeps = await _service.GetCheeps(PageNumber);
        HasNextPage = Cheeps.Count == 32;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return Forbid();

        if (string.IsNullOrWhiteSpace(Text) && Upload == null)
            ModelState.AddModelError(string.Empty, "You must write a cheep or upload an image.");

        if (!ModelState.IsValid)
        {
            PageNumber = 1;
            Cheeps = await _service.GetCheeps(PageNumber);
            HasNextPage = Cheeps.Count == 32;
            return Page();
        }

        string? imageUrl = null;
        if (Upload != null)
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(folder);

            var name = Guid.NewGuid() + "_" + Path.GetFileName(Upload.FileName);
            var path = Path.Combine(folder, name);

            await using var stream = new FileStream(path, FileMode.Create);
            await Upload.CopyToAsync(stream);

            imageUrl = "/uploads/" + name;
        }

        await CheepRepository.StoreCheepAsync(new MessageDTO
        {
            Text = Text ?? "",
            AuthorName = User.Identity!.Name!,
            TimeStamp = DateTime.UtcNow,
            ImageUrl = imageUrl
        });

        return RedirectToPage(new { p = 1 });
    }

    // ✅ Follow helpers for public timeline
    public async Task<bool> IsFollowingAsync(string name)
        => User.Identity?.IsAuthenticated == true
           && await CheepRepository.IsFollowingAsync(User.Identity.Name!, name);

    public async Task<IActionResult> OnGetFollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.FollowUserAsync(user, followee);
        return RedirectToPage(new { p = PageNumber });
    }

    public async Task<IActionResult> OnGetUnfollowAsync(string followee)
    {
        if (User.Identity?.Name is not { } user) return Forbid();
        await CheepRepository.UnfollowUserAsync(user, followee);
        return RedirectToPage(new { p = PageNumber });
    }
}
