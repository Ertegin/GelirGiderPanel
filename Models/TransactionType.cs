using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    /// <summary>
    /// İşlem türlerini tutar: Gelir (1) ve Gider (2).
    /// Lookup (referans) tablosu olarak çalışır, seed data ile doldurulur.
    /// </summary>
    public class TransactionType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tür adı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "İşlem Türü")]
        public string Name { get; set; } = null!;

        // Navigation property: Bu türe ait tüm işlemler
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
