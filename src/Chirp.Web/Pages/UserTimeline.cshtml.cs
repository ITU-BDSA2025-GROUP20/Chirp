using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
using Infrastructure.Data;

namespace Web.Pages;

public class UserTimelineModel : PageModel
{
    private readonly CheepService _service;
    public List<CheepViewModel> Cheeps { get; set; } = new();

    public UserTimelineModel(CheepService service)
    {
        _service = service;
    }

    public async Task<ActionResult> OnGet(string author, int? page = 1)
    {
        Cheeps = await _service.GetCheepsFromAuthor(author, page);

        
        return Page();
    }
}