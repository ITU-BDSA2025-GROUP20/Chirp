using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chirp.Razor.Services;
using Chirp.Razor.Data;

namespace Chirp.Razor.Pages;

public class UserTimelineModel : PageModel
{
    private readonly CheepService _service;
    public List<CheepViewModel> Cheeps { get; set; } = new();

    public UserTimelineModel(CheepService service)
    {
        _service = service;
    }

    public async Task<ActionResult> OnGet(string author)
    {
        Cheeps = await _service.GetCheepsFromAuthor(author);
        return Page();
    }
}