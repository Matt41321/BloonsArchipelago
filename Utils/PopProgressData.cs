using System.Collections.Generic;

namespace BloonsArchipelago.Utils
{
    public class PopProgressData
    {
        public Dictionary<string, long> CumulativePops { get; set; } = new();
        public List<string> PermanentlyUnlockedTiers { get; set; } = new();
    }
}
