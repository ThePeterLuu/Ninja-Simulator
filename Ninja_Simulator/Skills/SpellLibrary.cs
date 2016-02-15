using System;
using System.Collections.Generic;
using Ninja_Simulator.Entities;
using Ninja_Simulator.FFXIVConcepts;

namespace Ninja_Simulator.Skills
{
    public static class SpellLibrary
    {
        private static readonly Dictionary<Spells, double> Potencies = new Dictionary<Spells, double>
        {
            { Spells.DreamWithinADream, 300 },
            { Spells.Mug, 140 },
            { Spells.Jugulate, 80 },
            { Spells.TrickAttack, 400 },
            { Spells.FumaShuriken, 240 },
            { Spells.Raiton, 360 },
            { Spells.Suiton, 180 },
            { Spells.PrePullSuiton, 180 }
        };

        private static readonly Dictionary<Spells, long> Cooldowns = new Dictionary<Spells, long>
        {
            { Spells.DreamWithinADream, 90 },
            { Spells.Mug, 90 },
            { Spells.InternalRelease, 60 },
            { Spells.BloodForBlood, 80 },
            { Spells.Duality, 90 },
            { Spells.DexterityPotion, 270 },
            { Spells.Jugulate, 30 },
            { Spells.TrickAttack, 60 },
            { Spells.FumaShuriken, 20 },
            { Spells.Raiton, 20 },
            { Spells.Suiton, 20 },
            { Spells.Kassatsu, 120 },
            { Spells.Delay, 0 }
        };

        private static readonly Dictionary<Spells, DamageType> DamageTypes = new Dictionary<Spells, DamageType>
        {
            { Spells.DreamWithinADream, DamageType.Slashing },
            { Spells.Mug, DamageType.Slashing },
            { Spells.Jugulate, DamageType.Slashing },
            { Spells.TrickAttack, DamageType.Slashing },
            { Spells.FumaShuriken, DamageType.Slashing },
            { Spells.Raiton, DamageType.Magical },
            { Spells.Suiton, DamageType.Magical },
            { Spells.PrePullSuiton, DamageType.Magical }
        };

        private static readonly List<Spells> DamageSpells = new List<Spells>
        {
            Spells.DreamWithinADream,
            Spells.Mug,
            Spells.Jugulate,
            Spells.TrickAttack,
            Spells.FumaShuriken,
            Spells.Raiton,
            Spells.Suiton,
            Spells.PrePullSuiton
        };
        private static readonly List<Spells> BuffSpells = new List<Spells>
        {
            Spells.Kassatsu,
            Spells.Duality,
            Spells.InternalRelease,
            Spells.BloodForBlood,
            Spells.DexterityPotion,
            Spells.Delay
        };

        public static bool IsDamageSpell(Spells spell)
        {
            return DamageSpells.Contains(spell);
        }
        public static bool IsBuffSpell(Spells spell)
        {
            return BuffSpells.Contains(spell);
        }

        public static double SpellPotencies(Spells spell)
        {
            return Potencies[spell];
        }

        public static long SpellCooldowns(Spells spell)
        {
            return (long)TimeSpan.FromSeconds(Cooldowns[spell]).TotalMilliseconds;
        }

        public static DamageType SpellDamageType(Spells spell)
        {
            return DamageTypes[spell];
        }

        public static void QueueEffect(Player player, Spells spell, StrikingDummy target, bool verbose)
        {
            switch (spell)
            {
                case Spells.DreamWithinADream:
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.Mug:
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.InternalRelease:
                    player.QueuedEffects.Add(StatusEffects.InternalRelease, new EffectSnapshot { Duration = 650, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.BloodForBlood:
                    player.QueuedEffects.Add(StatusEffects.BloodForBlood, new EffectSnapshot { Duration = 650, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.Duality:
                    player.QueuedEffects.Add(StatusEffects.Duality, new EffectSnapshot { Duration = 650, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.DexterityPotion:
                    player.QueuedEffects.Add(StatusEffects.DexterityPotion, new EffectSnapshot { Duration = 949, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = 1334, Target = player });
                    break;
                case Spells.Jugulate:
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.TrickAttack:
                    target.QueuedEffects.Add(StatusEffects.TrickAttack, new EffectSnapshot { Duration = 955, Target = target });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.FumaShuriken:
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = 500 + GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.Raiton:
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = 1000 + GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.PrePullSuiton:
                    player.QueuedEffects.Add(StatusEffects.Suiton, new EffectSnapshot { Duration = 1000, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.Suiton:
                    player.QueuedEffects.Add(StatusEffects.Suiton, new EffectSnapshot { Duration = 2500, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = 1500 + GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.Kassatsu:
                    player.QueuedEffects.Add(StatusEffects.Kassatsu, new EffectSnapshot { Duration = 1000, Target = player });
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                case Spells.Delay:
                    player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    break;
                default:
                    throw new Exception($"Unknown spell { spell }!");
            }

            if (verbose)
            {
                var animationLockDuration = player.QueuedEffects[StatusEffects.AnimationLocked].Duration;
                var remainingGCD = player.GcdDuration;
                if (animationLockDuration > remainingGCD)
                {
                    Console.WriteLine($"Warning! GCD clipped by { spell } at { GameEngine.GetFormattedGameTime() } by { animationLockDuration - remainingGCD } milliseconds!");
                }
            }
        }

        public static void ApplyEffect(Actor target, Spells spell)
        {
            switch (spell)
            {
                case Spells.InternalRelease:
                    ApplyEffect(target, StatusEffects.InternalRelease, 15);
                    break;
                case Spells.BloodForBlood:
                    ApplyEffect(target, StatusEffects.BloodForBlood, 20);
                    break;
                case Spells.Duality:
                    ApplyEffect(target, StatusEffects.Duality, 10);
                    break;
                case Spells.DexterityPotion:
                    ApplyEffect(target, StatusEffects.DexterityPotion, 15);
                    break;
                case Spells.TrickAttack:
                    ApplyEffect(target, StatusEffects.TrickAttack, 10);
                    break;
                case Spells.Suiton:
                    ApplyEffect(target, StatusEffects.Suiton, 10);
                    break;
                case Spells.Kassatsu:
                    target.Cooldowns.Remove(Spells.FumaShuriken);
                    target.Cooldowns.Remove(Spells.Raiton);
                    target.Cooldowns.Remove(Spells.Suiton);
                    ApplyEffect(target, StatusEffects.Kassatsu, 15);
                    break;
            }
        }

        private static void ApplyEffect(Actor actor, StatusEffects statusEffect, long durationSeconds)
        {
            if (!(actor.StatusEffects.ContainsKey(statusEffect)))
            {
                actor.StatusEffects.Add(statusEffect, (long)TimeSpan.FromSeconds(durationSeconds).TotalMilliseconds);
            }
            else
            {
                actor.StatusEffects[statusEffect] = (long)TimeSpan.FromSeconds(durationSeconds).TotalMilliseconds;
            }
        }
    }
}
