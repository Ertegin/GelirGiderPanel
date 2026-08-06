using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    public class Salary
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "İsim zorunludur.")]
        [StringLength(150, ErrorMessage = "İsim en fazla 150 karakter olabilir.")]
        [Display(Name = "İsim")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Maaş tutarı zorunludur.")]
        [Range(1, 999999999, ErrorMessage = "Maaş sıfırdan büyük olmalıdır.")]
        [Display(Name = "Maaş (TL)")]
        public decimal Amount { get; set; }

        [StringLength(400, ErrorMessage = "Açıklama en fazla 400 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
