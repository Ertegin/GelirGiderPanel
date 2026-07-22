using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    /// <summary>
    /// İşlem kategorileri: Mutfak, Satış, Kira, Personel vb.
    /// </summary>
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Kategori Adı")]
        public string Name { get; set; } = null!;

        [StringLength(250)]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property: Bu kategoriye ait tüm işlemler
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
