using System.Collections.Generic;

namespace Ninja_Simulator.Entities
{
    public class RecordedDPS
    {
        public Player Player { get; set; }
        public double DPS { get; set; }
        public List<string> EquipmentNames { get; set; }
    }
}
