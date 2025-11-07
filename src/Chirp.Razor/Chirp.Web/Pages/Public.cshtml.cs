using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chirp.Infrastructure.Services;
using Chirp.Infrastructure.Data;

namespace Chirp.Razor.Pages;

public class PublicModel : PageModel
{
    private readonly CheepService _service;
    public List<CheepViewModel> Cheeps { get; set; } = new();

    public PublicModel(CheepService service)
    {
        _service = service;
    }

    public async Task<ActionResult> OnGet()
    {
        Cheeps = await _service.GetCheeps();
        return Page();
    }
}