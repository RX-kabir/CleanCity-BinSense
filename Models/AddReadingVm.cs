using System.ComponentModel.DataAnnotations;

namespace SmartWaste.Web.Models
{
    public class AddReadingVm
    {
        [Required]
        public int BinId { get; set; }

        [Required]
        [Range(0, 100)]
        public int FillLevelPercent { get; set; }

        public decimal? Temperature { get; set; }

        public DateTime? ReadingTime { get; set; }

        // dropdown support
        public List<(string Value, string Text)> Bins { get; set; } = new();
    }
}
