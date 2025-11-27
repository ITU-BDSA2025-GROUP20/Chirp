using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
using Infrastructure.Data;
using Infrastructure;
using Core;

namespace Web.Pages;

public class UserTimelineModel : PageModel
{
    private readonly CheepService _service;
    private readonly ICheepRepository _cheepRepository;

    public List<CheepViewModel> Cheeps { get; set; } = new();
    public string Author { get; set; } = string.Empty;

    [BindProperty]
    public string Text { get; set; } = string.Empty;

    public UserTimelineModel(CheepService service, ICheepRepository cheepRepository)
    {
        _service = service;
        _cheepRepository = cheepRepository;
    }

    public async Task<IActionResult> OnGetAsync(string author)
    {
        Author = author;
        Cheeps = await _service.GetCheepsFromAuthor(author);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string author)
    {
        if (!User.Identity.IsAuthenticated)
            return Forbid();

        if (!ModelState.IsValid)
        {
            Cheeps = await _service.GetCheepsFromAuthor(author);
            return Page();
        }

        var message = new MessageDTO
        {
            Text = Text,
            AuthorName = User.Identity.Name!,
            TimeStamp = DateTime.UtcNow
        };

        await _cheepRepository.StoreCheepAsync(message);

        return RedirectToPage(new { author });
    }
}