using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Q__Razor_.Models;
using Q__Razor_.Data;


// Sending the information to the server. (third method, using BindingProperty)
namespace Q__Razor_.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly Q__Razor_Context _context;

        public IndexModel(ILogger<IndexModel> logger, Q__Razor_Context context)
        {
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {

        }

        [BindProperty]
        public TblPlayers Player { get; set; } = new TblPlayers();
        public List<TblPlayers> Players { get; set; } = default!;

            public async Task<IActionResult> OnPostAsync()
            {
                if (ModelState.IsValid)
                {
                    // Add new player to the database
                    _context.TblPlayers.Add(Player);
                    await _context.SaveChangesAsync(); // Save changes to the database

                    // Redirect or display success message
                    return RedirectToPage(new { success = true });
                }

                // If ModelState is invalid, stay on the page and show errors
                return Page();
            }

     }
}