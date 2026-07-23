using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    public class CariTransaction
    {

        public int Id { get; set; }

        [Required]
        [Display(Name = "Cari Hesap")]

        // froegin key
        public int CariAccountId { get; set; }
        public CariAccount? CariAccount { get; set; }

        [Required(ErrorMessage = "Tarih zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Tarih")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Belge no en fazla 50 karakter olabilir.")]
        [Display(Name = "Belge / Fatura No")]
        public string? DocumentNo { get; set; }

        /// <summary>Miktar birimi: adet, metre, kg... (opsiyonel)</summary>
        [StringLength(20)]
        [Display(Name = "Birim")]
        public string? Unit { get; set; }

        /// <summary>
        /// Bu hareket kasaya (gelir-gider) da işlendiyse bağlı Transaction kaydının Id'si.
        /// Alacak → Gelir, Borç → Gider olarak yansır.
        /// </summary>
        public int? LinkedTransactionId { get; set; }

        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        /// <summary>Adet (opsiyonel; mal satırlarında kullanılır).</summary>
        [Range(0, int.MaxValue, ErrorMessage = "Miktar negatif olamaz.")]
        [Display(Name = "Miktar (Adet)")]
        public int? Quantity { get; set; }

        /// <summary>Birim fiyat (opsiyonel; mal satırlarında kullanılır).</summary>
        [Range(0, 999999999, ErrorMessage = "Birim fiyat negatif olamaz.")]
        [Display(Name = "Birim Fiyat (TL)")]
        public decimal? UnitPrice { get; set; }

        /// <summary>Borç / Giriş: mal teslimi vb. Müşterinin borcunu artırır.</summary>
        [Range(0, 999999999999, ErrorMessage = "Borç tutarı negatif olamaz.")]
        [Display(Name = "Borç (Giriş) TL")]
        public decimal DebitAmount { get; set; }

        /// <summary>Alacak / Çıkış: alınan ödeme vb. Müşterinin borcunu azaltır.</summary>
        [Range(0, 999999999999, ErrorMessage = "Alacak tutarı negatif olamaz.")]
        [Display(Name = "Alacak (Ödeme) TL")]
        public decimal CreditAmount { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

    }
}
