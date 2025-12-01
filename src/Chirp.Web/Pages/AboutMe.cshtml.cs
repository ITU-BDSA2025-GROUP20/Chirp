using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;

namespace Chirp.Web.Pages 
{
    public class AboutMeModel : PageModel
    {
        private readonly CheepService _service;

        public required string UserName { get; set; }
        public List<CheepViewModel> Cheeps { get; set; } = new();

        public AboutMeModel(CheepService service)
        {
            _service = service;
        }

        public async Task<IActionResult> OnGetAsync(int? page = 1)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/Public");
            }

            UserName = User.Identity!.Name!;

            Cheeps = await _service.GetCheepsFromAuthor(UserName, page);

            return Page();
        }
    }
}