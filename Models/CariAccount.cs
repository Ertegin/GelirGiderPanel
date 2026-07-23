using System.ComponentModel.DataAnnotations;

namespace GelirGiderPanel.Models
{
    public class CariAccount
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "Hesap adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Hesap adı en fazla 150 karakter olabilir.")]
        [Display(Name = "Hesap Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        /// <summary>
        /// Devir bakiyesi. Pozitif = müşteri bize borçlu, negatif = biz müşteriye borçluyuz.
        /// </summary>
        [Display(Name = "Devir Bakiyesi (TL)")]
        public decimal OpeningBalance { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //bir cari hesapta birden fazla hareket vardır   one to many
        public ICollection<CariTransaction> Transactions { get; set; } = new List<CariTransaction>();

    }
}
