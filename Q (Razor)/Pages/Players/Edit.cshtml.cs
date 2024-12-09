using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Q__Razor_.Data;
using Q__Razor_.Models;

namespace Q__Razor_.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly Q__Razor_.Data.Q__Razor_Context _context;

        public EditModel(Q__Razor_.Data.Q__Razor_Context context)
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

            var tblplayers =  await _context.TblPlayers.FirstOrDefaultAsync(m => m.ID == id);
            if (tblplayers == null)
            {
                return NotFound();
            }
            TblPlayers = tblplayers;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(TblPlayers).State = EntityState.Modified;

            
                await _context.SaveChangesAsync();
            
           

            return RedirectToPage("./Index");
        }

        private bool TblProductsExists(int id)
        {
          return (_context.TblPlayers?.Any(e => e.ID == id)).GetValueOrDefault();
        }
    }
}
