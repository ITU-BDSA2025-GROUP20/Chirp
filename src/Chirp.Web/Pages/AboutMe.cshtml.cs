using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Infrastructure.Services;
using Core;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Models;
using CheepViewModel = Infrastructure.Services.CheepViewModel;

namespace Chirp.Web.Pages 
{
    public class AboutMeModel : PageModel // More server side logic for the cshtml, to give data to the AboutMe page
    {
        private readonly CheepService _service;
        public required ICheepRepository CheepRepository { get; set; }

        public required string UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public List<CheepViewModel> Cheeps { get; set; } = new();
        public List<string> Following { get; set; } = new();

        public AboutMeModel(CheepService service, ICheepRepository cheepRepository)
        {
            _service = service;
            CheepRepository = cheepRepository;
        }

        public async Task<IActionResult> OnGetAsync(int? page = 1)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/Public");
            }

            UserName = User.Identity!.Name!;

            var author = await GetAuthorByNameAsync(UserName);

            Cheeps = await _service.GetCheepsFromAuthor(UserName, page);

            Following = (await CheepRepository.GetFollowingNamesAsync(UserName)).ToList();

            return Page();
        }

        private async Task<Author?> GetAuthorByNameAsync(string name)
    {
        var dbField = CheepRepository.GetType()
            .GetField("_dbcontext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (dbField?.GetValue(CheepRepository) is not CheepDbContext db) return null;

        return await db.Authors
            .FirstOrDefaultAsync(a => a.Name == name);
    }
    }
}
