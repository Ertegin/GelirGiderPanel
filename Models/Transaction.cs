using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GelirGiderPanel.Models
{
    /// <summary>
    /// Gelir ve Gider hareketlerinin tutulduğu ana işlem tablosu.
    /// Örn: "Mutfak Gideri - 100 TL", "Gömlek Satış Geliri - 1500 TL"
    /// </summary>
    public class Transaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tutar zorunludur.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999, ErrorMessage = "Tutar 0'dan büyük olmalıdır.")]
        [Display(Name = "Tutar (₺)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(250)]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Tarih zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "İşlem Tarihi")]
        public DateTime Date { get; set; } = DateTime.Today;

        // ----- Foreign Keys -----

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "İşlem türü (Gelir/Gider) zorunludur.")]
        [Display(Name = "İşlem Türü")]
        public int TransactionTypeId { get; set; }

        [ForeignKey(nameof(TransactionTypeId))]
        public TransactionType? TransactionType { get; set; }

        [Display(Name = "Kayıt Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
