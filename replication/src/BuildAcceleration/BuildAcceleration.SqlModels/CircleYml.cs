using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.SqlModels
{
    [Index(nameof(Sha256))]
    public class CircleYml
    {
        public int Id { get; set; }
        //[MaxLength(32)]
        public byte[] Sha256 { get; set; } = null!;
        public string Content { get; set; } = null!;

        //public List<BuildEntrySlim> Builds { get; set; } = null!;
    }
}
