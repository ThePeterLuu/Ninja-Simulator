using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Ninja_Simulator.Formulas
{
    public static class FormulaLibrary
    {
        private const string SkillSpeedRanksFilePath = "SkillSpeedRankings.txt";

        private static IDictionary<int, int> _sortedSkillSpeedRanks;

        public static double WeaponSkills(double potency, double weaponDamage, double dex, double det, double multiplier)
        {
            return ((potency / 100) * (1 + weaponDamage * 0.0432544) * (dex * 0.1027246) * (1 + det / 7290) * multiplier) - 2;
        }

        public static double AutoAttack(double aaDmg, double dex, double det, double aaDelay, double multiplier)
        {
            return ((aaDmg * 0.0593365 + 1) * (dex * 0.0841892) * (det * 0.0001464 + 1) * multiplier) - 1;
        }

        public static double SkillSpeedMultiplier(double sks)
        {
            return ((sks - 354) / 7722 + 1);
        }

        public static double Gcd(double sks, double multiplier)
        {
            return (2.50245 - ((sks - 354) * 0.0003776)) * multiplier;
        }

        public static double CritChance(double crt)
        {
            return ((crt - 354) / (858 * 5)) + 0.05;
        }

        public static double CritDmg(double crt)
        {
            return ((crt - 354) / (858 * 5)) + 1.45;
        }

        public static string GetSkillSpeedRanksFilePath()
        {
            return SkillSpeedRanksFilePath;
        }

        public static int GetSkillSpeedRank(double sks)
        {
            if (_sortedSkillSpeedRanks == null)
            {
                _sortedSkillSpeedRanks = GetSkillSpeedRanks();
            }

            return _sortedSkillSpeedRanks[(int)sks];
        }

        private static IDictionary<int, int> GetSkillSpeedRanks()
        {
            using (var file = File.OpenRead(SkillSpeedRanksFilePath))
            using (var reader = new StreamReader(file))
            {
                var result = new Dictionary<int, int>();
                var sksToDPS = JsonConvert.DeserializeObject<Dictionary<int, double>>(reader.ReadToEnd());
                var orderedSksToDPS = sksToDPS
                    .ToDictionary(mapping => mapping.Key, mapping => (int)mapping.Value)
                    .OrderBy(kvp => kvp.Value).ToList();

                var currentRank = 0;
                var lastDPSValue = 0;
                foreach (var pair in orderedSksToDPS)
                {
                    if (pair.Value > lastDPSValue)
                    {
                        currentRank++;
                        lastDPSValue = pair.Value;
                    }

                    result.Add(pair.Key, currentRank);
                }

                return result;
            }
        }
    }
}
