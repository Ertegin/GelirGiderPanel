using GelirGiderPanel.Enums;
using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    public class AppUser
    {
        public int ID { get; set; }


        [Required(ErrorMessage = "İsim zorunludur.")]
        [Display(Name = "İsim")]
        public string UserName { get; set; }


        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }
        public Role Role { get; set; }
    }
}
