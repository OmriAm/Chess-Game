using System.ComponentModel.DataAnnotations;

namespace Q__Razor_.Models
{
    public class TblPlayers
    {
        public int ID { get; set; } // Ensure ID is defined

        [StringLength(60, MinimumLength = 2, ErrorMessage = "Minimum 2 chars")]
        [Display(Name = "The Name")]
        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }

        [RegularExpression(@"^\+?\d{9,9}$", ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        [Display(Name = "Your Country")]
        public string? Country { get; set; } // Ensure Country is defined
    }
}
