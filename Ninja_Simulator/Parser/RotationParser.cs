using System;
using System.Configuration;
using System.IO;
using Ninja_Simulator.Entities;
using Ninja_Simulator.Skills;

namespace Ninja_Simulator.Parser
{
    public class RotationParser
    {
        private static string[] _loadedRotation;
        private static int _currentAbility;
        private static readonly bool LoopRotationInfinitely = Convert.ToBoolean(ConfigurationManager.AppSettings["LoopRotationInfinitely"]);

        public static void LoadRotation(int sks)
        {
            _loadedRotation = File.ReadAllLines(ConfigurationManager.AppSettings["StandardRotation"]);
        }

        public static Enum SelectFirstAbility(Actor target, bool verbose = false)
        {
            _currentAbility = 0;
            return SelectNextAbility(target, true, verbose);
        }

        public static Enum SelectNextAbility(Actor target, bool firstAbility = false, bool verbose = false)
        {
            var selectedSkill = ParseAbility(_loadedRotation[_currentAbility], firstAbility);

            _currentAbility++;

            if (_currentAbility == _loadedRotation.Length && LoopRotationInfinitely)
            {
                _currentAbility = 0;
            }

            if (selectedSkill is Spells)
            {
                if ((Spells) selectedSkill == Spells.EnableFoeRequiem)
                {
                    if (!target.StatusEffects.ContainsKey(StatusEffects.FoeRequiem))
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Foe Requiem applied!");
                        }
                        target.StatusEffects.Add(StatusEffects.FoeRequiem, long.MaxValue);
                    }
                    // ReSharper disable once TailRecursiveCall
                    return SelectNextAbility(target, firstAbility);
                }
                if ((Spells) selectedSkill == Spells.DisableFoeRequiem)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Foe Requiem removed!");
                    }
                    target.StatusEffects.Remove(StatusEffects.FoeRequiem);
                    // ReSharper disable once TailRecursiveCall
                    return SelectNextAbility(target, firstAbility);
                }
                if ((Spells) selectedSkill == Spells.EnableStormsEye)
                {
                    if (!target.StatusEffects.ContainsKey(StatusEffects.StormsEye))
                    {
                        if (verbose)
                        {
                            Console.WriteLine("Storm's Eye applied!");
                        }
                        target.StatusEffects.Add(StatusEffects.StormsEye, long.MaxValue);
                        target.StatusEffects.Remove(StatusEffects.DancingEdge);
                    }
                    // ReSharper disable once TailRecursiveCall
                    return SelectNextAbility(target, firstAbility);
                }
                if ((Spells) selectedSkill == Spells.DisableStormsEye)
                {
                    if (verbose)
                    {
                        Console.WriteLine("Storm's Eye removed!");
                    }
                    target.StatusEffects.Remove(StatusEffects.StormsEye);
                    // ReSharper disable once TailRecursiveCall
                    return SelectNextAbility(target, firstAbility);
                }
            }

            return selectedSkill;
        }

        private static Enum ParseAbility(string ability, bool firstAbility = false)
        {
            switch (ability)
            {
                case "EYEON":
                    return Spells.EnableStormsEye;
                case "EYEOFF":
                    return Spells.DisableStormsEye;
                case "FOEON":
                    return Spells.EnableFoeRequiem;
                case "FOEOFF":
                    return Spells.DisableFoeRequiem;
                case "SE":
                    return WeaponSkills.SpinningEdge;
                case "GS":
                    return WeaponSkills.GustSlash;
                case "MU":
                    return WeaponSkills.Mutilate;
                case "AE":
                    return WeaponSkills.AeolianEdge;
                case "DE":
                    return WeaponSkills.DancingEdge;
                case "SF":
                    return WeaponSkills.ShadowFang;
                case "AC":
                    return WeaponSkills.ArmorCrush;
                case "MUG":
                    return Spells.Mug;
                case "JUG":
                    return Spells.Jugulate;
                case "TA":
                    return Spells.TrickAttack;
                case "KAS":
                    return Spells.Kassatsu;
                case "DUA":
                    return Spells.Duality;
                case "DEXPOT":
                    return Spells.DexterityPotion;
                case "DWD":
                    return Spells.DreamWithinADream;
                case "FUMA":
                    return Spells.FumaShuriken;
                case "RAITON":
                    return Spells.Raiton;
                case "PPSUITON":
                    if (!firstAbility)
                    {
                        throw new Exception($"Cannot use { ability } other than as the first ability.");
                    }
                    return Spells.PrePullSuiton;
                case "SUITON":
                    return Spells.Suiton;
                case "B4B":
                    return Spells.BloodForBlood;
                case "IR":
                    return Spells.InternalRelease;
                case "DELAY":
                    return Spells.Delay;
                default:
                    throw new Exception($"Unrecognized ability: { ability }");
            }
        }
    }
}
