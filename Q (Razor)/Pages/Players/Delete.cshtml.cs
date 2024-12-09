using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Q__Razor_.Data;
using Q__Razor_.Models;

namespace Q__Razor_.Pages.Products
{
    public class DeleteModel : PageModel
    {
        private readonly Q__Razor_.Data.Q__Razor_Context _context;

        public DeleteModel(Q__Razor_.Data.Q__Razor_Context context)
        {
            _context = context;
        }

        [BindProperty]
      public TblPlayers TblPlayers { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.TblPlayers == null)
            {
                return NotFound();
            }

            var tblplayers = await _context.TblPlayers.FirstOrDefaultAsync(m => m.ID == id);

            if (tblplayers == null)
            {
                return NotFound();
            }
            else 
            {
                TblPlayers = tblplayers;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.TblPlayers == null)
            {
                return NotFound();
            }
            var tblplayers = await _context.TblPlayers.FindAsync(id);

            if (tblplayers != null)
            {
                TblPlayers = tblplayers;
                _context.TblPlayers.Remove(TblPlayers);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
