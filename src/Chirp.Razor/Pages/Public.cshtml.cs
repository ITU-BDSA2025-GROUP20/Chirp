using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Razor.Pages;

public class PublicModel : PageModel
{
    private readonly CheepService _service;
    public List<CheepViewModel> Cheeps { get; set; }

    public PublicModel(CheepService service)
    {
        _service = service;
    }

    public ActionResult OnGet()
    {
        Cheeps = _service.GetCheeps();
        return Page();
    }
}