using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ninja_Simulator.Entities;
using Ninja_Simulator.FFXIVConcepts;

namespace Ninja_Simulator.Skills
{
    public static class WeaponLibrary
    {
        private static readonly Dictionary<WeaponSkills, double> Potencies = new Dictionary<WeaponSkills, double>
        {
            { WeaponSkills.SpinningEdge, 150 },
            { WeaponSkills.ShadowFang, 100 },
            { WeaponSkills.GustSlash, 100 },
            { WeaponSkills.AeolianEdge, 180 },
            { WeaponSkills.DancingEdge, 100 },
            { WeaponSkills.ArmorCrush, 160 },
            { WeaponSkills.Mutilate, 60 }
        };

        public static double WeaponPotencies(WeaponSkills weaponSkill, Stack<WeaponSkills> lastSkills)
        {
            if (weaponSkill == WeaponSkills.ShadowFang)
            {
                if (lastSkills.Peek() == WeaponSkills.SpinningEdge)
                {
                    return 200;
                }
            }

            if (weaponSkill == WeaponSkills.GustSlash)
            {
                if (lastSkills.Peek() == WeaponSkills.SpinningEdge)
                {
                    return 200;
                }
            }

            if (weaponSkill == WeaponSkills.AeolianEdge)
            {
                var lastSkill = lastSkills.Pop();
                if (lastSkill == WeaponSkills.GustSlash && lastSkills.Peek() == WeaponSkills.SpinningEdge)
                {
                    lastSkills.Push(lastSkill);
                    return 320;
                }
                lastSkills.Push(lastSkill);
            }

            if (weaponSkill == WeaponSkills.DancingEdge)
            {
                var lastSkill = lastSkills.Pop();
                if (lastSkill == WeaponSkills.GustSlash && lastSkills.Peek() == WeaponSkills.SpinningEdge)
                {
                    lastSkills.Push(lastSkill);
                    return 260;
                }
                lastSkills.Push(lastSkill);
            }

            if (weaponSkill == WeaponSkills.ArmorCrush)
            {
                var lastSkill = lastSkills.Pop();
                if (lastSkill == WeaponSkills.GustSlash && lastSkills.Peek() == WeaponSkills.SpinningEdge)
                {
                    lastSkills.Push(lastSkill);
                    return 280;
                }
                lastSkills.Push(lastSkill);
            }

            return Potencies[weaponSkill];
        }

        public static void QueueEffect(Player player, StrikingDummy strikingDummy, WeaponSkills weaponSkill)
        {
            WeaponSkills lastSkill;
            player.QueuedEffects.Add(StatusEffects.AnimationLocked, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });

            switch (weaponSkill)
            {
                case WeaponSkills.ShadowFang:
                    if (player.LastSkills.Peek() == WeaponSkills.SpinningEdge)
                    {
                        strikingDummy.QueuedEffects.Add(StatusEffects.ShadowFang, new EffectSnapshot
                        {
                            Duration = 585,
                            CritChance = player.CalculateCritChance(),
                            Crt = player.Crt,
                            Det = player.Det,
                            Multiplier = player.CalculateDamageOverTimeMultiplier(DamageType.Physical, strikingDummy),
                            Dex = player.GetDexterity(),
                            Potency = 40,
                            Target = strikingDummy,
                            WeaponDamage = player.Weapon.WeaponDamage
                        });
                    }
                    break;
                case WeaponSkills.Mutilate:
                    strikingDummy.QueuedEffects.Add(StatusEffects.Mutilate, new EffectSnapshot
                    {
                        Duration = 400,
                        CritChance = player.CalculateCritChance(),
                        Crt = player.Crt,
                        Det = player.Det,
                        Multiplier = player.CalculateDamageOverTimeMultiplier(DamageType.Physical, strikingDummy),
                        Dex = player.GetDexterity(),
                        Potency = 30,
                        Target = strikingDummy,
                        WeaponDamage = player.Weapon.WeaponDamage
                    });
                    break;
                case WeaponSkills.DancingEdge:
                    lastSkill = player.LastSkills.Pop();
                    if (lastSkill == WeaponSkills.GustSlash && player.LastSkills.Peek() == WeaponSkills.SpinningEdge)
                    {
                        strikingDummy.QueuedEffects.Add(StatusEffects.DancingEdge, new EffectSnapshot { Duration = 395, Target = strikingDummy });
                    }
                    player.LastSkills.Push(lastSkill);
                    break;
                case WeaponSkills.ArmorCrush:
                    lastSkill = player.LastSkills.Pop();
                    if (lastSkill == WeaponSkills.GustSlash && player.LastSkills.Peek() == WeaponSkills.SpinningEdge)
                    {
                        player.QueuedEffects.Add(StatusEffects.ArmorCrush, new EffectSnapshot { Duration = GameEngine.GetGlobalAnimationLockDurationMs(), Target = player });
                    }
                    player.LastSkills.Push(lastSkill);
                    break;
            }
        }

        public static void ApplyEffect(Actor target, WeaponSkills weaponSkill, bool verbose, EffectSnapshot effectSnapshot = null)
        {
            switch (weaponSkill)
            {
                case WeaponSkills.ArmorCrush:
                    if (target.StatusEffects.ContainsKey(StatusEffects.Huton))
                    {
                        var newHutonDuration = Math.Min(target.StatusEffects[StatusEffects.Huton] + (long)TimeSpan.FromSeconds(30).TotalMilliseconds, 70000);
                        ApplyEffect(target, StatusEffects.Huton, newHutonDuration, verbose);
                    }
                    break;
                case WeaponSkills.DancingEdge:
                    ApplyEffect(target, StatusEffects.DancingEdge, 20000, verbose);
                    break;
                case WeaponSkills.Mutilate:
                    Debug.Assert(effectSnapshot != null, "effectSnapshot != null");
                    effectSnapshot.Duration = (long)TimeSpan.FromSeconds(30).TotalMilliseconds;
                    ApplyDamageOverTime(target, StatusEffects.Mutilate, effectSnapshot, verbose);
                    break;
                case WeaponSkills.ShadowFang:
                    Debug.Assert(effectSnapshot != null, "effectSnapshot != null");
                    effectSnapshot.Duration = (long)TimeSpan.FromSeconds(18).TotalMilliseconds;
                    ApplyDamageOverTime(target, StatusEffects.ShadowFang, effectSnapshot, verbose);
                    break;
            }
        }

        private static void ApplyEffect(Actor actor, StatusEffects statusEffect, long durationMs, bool verbose)
        {
            if (!(actor.StatusEffects.ContainsKey(statusEffect)))
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{statusEffect} applied!");
                }

                actor.StatusEffects.Add(statusEffect, durationMs);
            }
            else
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{statusEffect} refreshed!");
                    if (statusEffect == StatusEffects.Huton)
                    {
                        Console.WriteLine($"Current Huton Duration: { durationMs } milliseconds.");
                    }
                }

                actor.StatusEffects[statusEffect] = durationMs;
            }
        }

        private static void ApplyDamageOverTime(Actor target, StatusEffects statusEffect, EffectSnapshot effectSnapshot, bool verbose)
        {
            if (!(target.DamageOverTimeEffects.ContainsKey(statusEffect)))
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{statusEffect} applied!");
                }

                target.DamageOverTimeEffects.Add(statusEffect, effectSnapshot);
            }
            else
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{statusEffect} refreshed!");
                }

                target.DamageOverTimeEffects[statusEffect] = effectSnapshot;
            }
        }
    }
}
