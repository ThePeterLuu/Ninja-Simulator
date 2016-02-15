using Ninja_Simulator.Entities;

namespace Ninja_Simulator.FFXIVConcepts
{
    public class EffectSnapshot
    {
        public Actor Caster { get; set; }
        public Actor Target { get; set; }
        public long Duration { get; set; }
        public double Potency { get; set; }
        public double Multiplier { get; set; }
        public double Dex { get; set; }
        public double WeaponDamage { get; set; }
        public double Det { get; set; }
        public double Crt { get; set; }
        public double CritChance { get; set; }
    }
}
