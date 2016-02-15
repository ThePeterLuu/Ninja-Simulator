using System;
using System.Collections.Generic;
using System.Linq;
using Ninja_Simulator.FFXIVConcepts;
using Ninja_Simulator.Formulas;
using Ninja_Simulator.Skills;

namespace Ninja_Simulator.Entities
{
    public abstract class Actor
    {
        public Dictionary<StatusEffects, EffectSnapshot> DamageOverTimeEffects = new Dictionary<StatusEffects, EffectSnapshot>();
        public Dictionary<StatusEffects, long> StatusEffects = new Dictionary<StatusEffects, long>();
        public Dictionary<StatusEffects, EffectSnapshot> QueuedEffects = new Dictionary<StatusEffects, EffectSnapshot>();
        public Dictionary<Spells, long> Cooldowns = new Dictionary<Spells, long>();
        public double DamageTaken { get; set; }

        public void DecrementCooldownDuration(bool verbose)
        {
            foreach (var cooldown in Cooldowns.ToList())
            {
                Cooldowns[cooldown.Key] = cooldown.Value - 1;
                if (Cooldowns[cooldown.Key] <= 0)
                {
                    Cooldowns.Remove(cooldown.Key);
                }
            }
        }

        public void DecrementStatusEffectDuration(bool verbose)
        {
            foreach (var effect in StatusEffects.ToList())
            {
                StatusEffects[effect.Key] = effect.Value - 1;
                if (StatusEffects[effect.Key] <= 0)
                {
                    if (verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{effect.Key} has fallen off!");
                    }

                    StatusEffects.Remove(effect.Key);
                }
            }
        }

        public void DecrementQueuedEffectDuration(bool verbose)
        {
            foreach (var queuedEffect in QueuedEffects.ToList())
            {
                var effect = QueuedEffects[queuedEffect.Key];
                effect.Duration = effect.Duration - 1;
                QueuedEffects[queuedEffect.Key] = effect;

                if (effect.Duration <= 0)
                {
                    try
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var effectName = Enum.GetName(typeof(StatusEffects), queuedEffect.Key);

                        WeaponSkills weaponSkill;
                        Spells spell;
                        if (Enum.TryParse(effectName, true, out weaponSkill))
                        {
                            WeaponLibrary.ApplyEffect(effect.Target, weaponSkill, verbose, queuedEffect.Value);
                        }

                        if (Enum.TryParse(effectName, true, out spell))
                        {
                            SpellLibrary.ApplyEffect(effect.Target, spell);
                        }
                    }
                    catch
                    {
                        // ignored if it's another non-actionable type such as AnimationLocked
                    }
                    finally
                    {
                        QueuedEffects.Remove(queuedEffect.Key);
                    }
                }
            }
        }

        public void DecrementDamageOverTimeDuration(bool verbose)
        {
            foreach (var dot in DamageOverTimeEffects.ToList())
            {
                var dotEffect = DamageOverTimeEffects[dot.Key];

                if (GameEngine.GetCurrentGameTime() % (long)TimeSpan.FromSeconds(3).TotalMilliseconds == 0)
                {
                    var damage = FormulaLibrary.WeaponSkills(dotEffect.Potency, dotEffect.WeaponDamage, dotEffect.Dex, dotEffect.Det, dotEffect.Multiplier);

                    damage = (damage * dotEffect.CritChance * FormulaLibrary.CritDmg(dotEffect.Crt)) +
                             (damage * (1 - dotEffect.CritChance));

                    if (verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{dot.Key} ticks for {damage}!");
                    }

                    dotEffect.Target.DamageTaken += damage;
                }

                dotEffect.Duration--;
                if (dotEffect.Duration <= 0)
                {
                    if (verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"{dot.Key} has fallen off!");
                    }

                    DamageOverTimeEffects.Remove(dot.Key);
                }
                else
                {
                    DamageOverTimeEffects[dot.Key] = dotEffect;
                }
            }
        }
    }
}
