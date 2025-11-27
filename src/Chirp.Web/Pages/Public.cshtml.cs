using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
using Infrastructure.Data;
using Infrastructure;
using Core;

namespace Web.Pages;
public class PublicModel : PageModel
{
    private readonly CheepService _service;
    private readonly ICheepRepository _cheepRepository;
    public List<CheepViewModel> Cheeps { get; set; } = new();
    [BindProperty]
    public string Text { get; set; } = string.Empty;

    public PublicModel(CheepService service, ICheepRepository cheepRepository)
    {
        _service = service;
        _cheepRepository = cheepRepository;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Cheeps = await _service.GetCheeps();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.Identity.IsAuthenticated)
            return Forbid();

        if (!ModelState.IsValid)
        {
            Cheeps = await _service.GetCheeps();
            return Page();
        }

        var message = new MessageDTO
        {
            Text = Text,
            AuthorName = User.Identity.Name!,
            TimeStamp = DateTime.UtcNow
        };

        await _cheepRepository.StoreCheepAsync(message);
        return RedirectToPage(); // Refresh page
    }
}