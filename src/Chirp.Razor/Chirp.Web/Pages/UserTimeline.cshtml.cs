using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chirp.Infrastructure.Services;
using Chirp.Infrastructure.Data;

namespace Chirp.Web.Pages;

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