using System;
using System.Collections.Generic;
using Ninja_Simulator.FFXIVConcepts;
using Ninja_Simulator.Formulas;
using Ninja_Simulator.Skills;

namespace Ninja_Simulator.Entities
{
    public class Player : Actor
    {
        // Constructor
        public Player()
        {
            LastSkills = new Stack<WeaponSkills>();
        }

        // GCD
        public long AutoAttackDuration { get; set; }
        public long GcdDuration { get; set; }

        // Weapon Properties
        public Weapon Weapon { get; set; }

        // Attributes
        public double Dex { get; set; }
        public double Vit { get; set; }
        public double Int { get; set; }
        public double Mnd { get; set; }
        public double Pie { get; set; }

        // Offensive Properties
        public double Acc { get; set; }
        public double Crt { get; set; }
        public double Det { get; set; }

        // Physical Properties
        public double Sks { get; set; }

        // Misc
        public Stack<WeaponSkills> LastSkills { get; set; }

        // -- Methods

        public double AutoAttack(StrikingDummy target)
        {
            if (AutoAttackDuration > 0)
            {
                return 0;
            }

            var damage = FormulaLibrary.AutoAttack(Weapon.AutoAttack, GetDexterity(), Det, Weapon.Delay, CalculateMultiplier(target, DamageType.Slashing));

            damage = (damage * CalculateCritChance() * FormulaLibrary.CritDmg(Crt)) +
                             (damage * (1 - CalculateCritChance()));

            AutoAttackDuration = StatusEffects.ContainsKey(Skills.StatusEffects.Huton) ? 
                (int)(TimeSpan.FromSeconds(Weapon.Delay).TotalMilliseconds * 0.85) :
                (int)TimeSpan.FromSeconds(Weapon.Delay).TotalMilliseconds;

            return damage;
        }

        public double Attack(StrikingDummy target, WeaponSkills weaponSkill)
        {
            var animationLocked = QueuedEffects.ContainsKey(Skills.StatusEffects.AnimationLocked);
            if (GcdDuration > 0 || animationLocked)
            {
                return 0;
            }

            var potency = WeaponLibrary.WeaponPotencies(weaponSkill, LastSkills);
            var damage = FormulaLibrary.WeaponSkills(potency, Weapon.WeaponDamage, GetDexterity(), Det, CalculateMultiplier(target, DamageType.Slashing));

            if (StatusEffects.ContainsKey(Skills.StatusEffects.Duality))
            {
                damage *= 2;
                StatusEffects.Remove(Skills.StatusEffects.Duality);
            }
            else
            {
                damage = (damage * CalculateCritChance() * FormulaLibrary.CritDmg(Crt)) +
                        (damage * (1 - CalculateCritChance()));
            }

            WeaponLibrary.QueueEffect(this, target, weaponSkill);
            var gcdMultiplier = StatusEffects.ContainsKey(Skills.StatusEffects.Huton) ? 0.85 : 1.00;
            GcdDuration = (int)TimeSpan.FromSeconds(FormulaLibrary.Gcd(Sks, gcdMultiplier)).TotalMilliseconds;
            LastSkills.Push(weaponSkill);

            return damage;
        }

        public double UseDamageSpell(StrikingDummy target, Spells spell, bool verbose = false)
        {
            if (GcdDuration <= 0 && spell != Spells.PrePullSuiton)
            {
                var remainingCd = Cooldowns.ContainsKey(spell) ? Cooldowns[spell] : 0;
                throw new Exception($"Warning! Using off-gcd ability { spell } when GCD is available! Remaining cooldown on { spell }: { remainingCd }");
            }

            if (Cooldowns.ContainsKey(spell) || QueuedEffects.ContainsKey(Skills.StatusEffects.AnimationLocked))
            {
                return 0;
            }

            if (spell == Spells.FumaShuriken ||
                spell == Spells.Raiton ||
                spell == Spells.Suiton)
            {
                if (Cooldowns.ContainsKey(Spells.FumaShuriken) ||
                    Cooldowns.ContainsKey(Spells.Raiton) ||
                    Cooldowns.ContainsKey(Spells.Suiton))
                {
                    return 0;
                }
            }

            if (spell == Spells.TrickAttack)
            {
                if (!StatusEffects.ContainsKey(Skills.StatusEffects.Suiton))
                {
                    throw new Exception($"Warning! Cannot use TA without Suiton active!");
                }
                if (verbose)
                {
                    Console.WriteLine("Suiton removed after TA cast!");
                }
                StatusEffects.Remove(Skills.StatusEffects.Suiton);
            }

            var potency = SpellLibrary.SpellPotencies(spell);
            var multiplier = CalculateMultiplier(target, SpellLibrary.SpellDamageType(spell));
            var damage = FormulaLibrary.WeaponSkills(potency, Weapon.WeaponDamage, GetDexterity(), Det, multiplier);

            var guaranteeCrit = false;

            if (spell == Spells.FumaShuriken ||
                spell == Spells.Raiton ||
                spell == Spells.Suiton)
            {
                if (StatusEffects.ContainsKey(Skills.StatusEffects.Kassatsu))
                {
                    guaranteeCrit = true;
                    StatusEffects.Remove(Skills.StatusEffects.Kassatsu);
                }
            }

            if (guaranteeCrit)
            {
                damage *= FormulaLibrary.CritDmg(Crt);
            }
            else
            {
                damage = (damage * CalculateCritChance() * FormulaLibrary.CritDmg(Crt)) +
                    (damage * (1 - CalculateCritChance()));
            }
            
            SpellLibrary.QueueEffect(this, spell, target, verbose);

            if (spell == Spells.FumaShuriken)
            {
                Cooldowns.Add(spell, 500 + SpellLibrary.SpellCooldowns(spell));
            }
            else if (spell == Spells.Raiton)
            {
                Cooldowns.Add(spell, 1000 + SpellLibrary.SpellCooldowns(spell));
            }
            else if (spell == Spells.Suiton)
            {
                Cooldowns.Add(spell, 1500 + SpellLibrary.SpellCooldowns(spell));
            }
            else if (spell == Spells.PrePullSuiton)
            {
                Cooldowns.Add(Spells.Suiton, SpellLibrary.SpellCooldowns(Spells.Suiton));
            }
            else
            {
                Cooldowns.Add(spell, SpellLibrary.SpellCooldowns(spell));
            }

            return damage;
        }

        public bool UseBuffSpell(StrikingDummy target, Spells spell, bool verbose = false)
        {
            if (GcdDuration <= 0)
            {
                throw new Exception($"Warning! Using off-gcd ability { spell } when GCD is available! Remaining cooldown on { spell }: { Cooldowns[spell]}");
            }

            if (Cooldowns.ContainsKey(spell) || QueuedEffects.ContainsKey(Skills.StatusEffects.AnimationLocked))
            {
                return false;
            }

            SpellLibrary.QueueEffect(this, spell, target, verbose);
            Cooldowns.Add(spell, SpellLibrary.SpellCooldowns(spell));
            return true;
        }

        private double CalculateMultiplier(Actor target, DamageType damageType)
        {
            var multiplier = 1.00;

            if (StatusEffects.ContainsKey(Skills.StatusEffects.BloodForBlood))
            {
                multiplier *= 1.10;
            }

            if (target.StatusEffects.ContainsKey(Skills.StatusEffects.TrickAttack))
            {
                multiplier *= 1.10;
            }

            if (damageType == DamageType.Slashing)
            {
                if (target.StatusEffects.ContainsKey(Skills.StatusEffects.DancingEdge) ||
                    target.StatusEffects.ContainsKey(Skills.StatusEffects.StormsEye))
                {
                    multiplier *= 1.10;
                }
            }

            if (damageType == DamageType.Physical || damageType == DamageType.Slashing)
            {
                if (StatusEffects.ContainsKey(Skills.StatusEffects.KissOfTheWasp))
                {
                    multiplier *= 1.20;
                }
            }

            if (damageType == DamageType.Magical)
            {
                if (target.StatusEffects.ContainsKey(Skills.StatusEffects.FoeRequiem))
                {
                    multiplier *= 1.10;
                }
            }

            return multiplier;
        }

        public double CalculateCritChance()
        {
            var critChance = FormulaLibrary.CritChance(Crt);

            if (StatusEffects.ContainsKey(Skills.StatusEffects.InternalRelease))
            {
                critChance += 0.10;
            }

            return critChance;
        }

        public double CalculateDamageOverTimeMultiplier(DamageType damageType, Actor target)
        {
            var multiplier = FormulaLibrary.SkillSpeedMultiplier(Sks);

            if (StatusEffects.ContainsKey(Skills.StatusEffects.BloodForBlood))
            {
                multiplier *= 1.10;
            }

            if (target.StatusEffects.ContainsKey(Skills.StatusEffects.TrickAttack))
            {
                multiplier *= 1.10;
            }

            if (damageType == DamageType.Slashing)
            {
                if (target.StatusEffects.ContainsKey(Skills.StatusEffects.DancingEdge) ||
                    target.StatusEffects.ContainsKey(Skills.StatusEffects.StormsEye))
                {
                    multiplier *= 1.10;
                }
            }

            if (damageType == DamageType.Physical || damageType == DamageType.Slashing)
            {
                if (StatusEffects.ContainsKey(Skills.StatusEffects.KissOfTheWasp))
                {
                    multiplier *= 1.20;
                }
            }

            if (damageType == DamageType.Magical)
            {
                if (target.StatusEffects.ContainsKey(Skills.StatusEffects.FoeRequiem))
                {
                    multiplier *= 1.10;
                }
            }

            return multiplier;
        }

        public double GetDexterity()
        {
            return StatusEffects.ContainsKey(Skills.StatusEffects.DexterityPotion)
                ? Dex + Math.Min(Dex * 0.20, 105)
                : Dex;
        }
    }
}
