using System;
using System.Configuration;
using Ninja_Simulator.Entities;
using Ninja_Simulator.Skills;

namespace Ninja_Simulator.Factories
{
    public class PlayerFactory
    {
        public static Player CreatePlayer(bool baseStatsOnly = false)
        {
            var player = new Player();
            if (baseStatsOnly)
            {
                player.Dex = Convert.ToDouble(ConfigurationManager.AppSettings["BASE_DEX"]);
                player.Acc = Convert.ToDouble(ConfigurationManager.AppSettings["BASE_ACC"]);
                player.Crt = Convert.ToDouble(ConfigurationManager.AppSettings["BASE_CRT"]);
                player.Det = Convert.ToDouble(ConfigurationManager.AppSettings["BASE_DET"]);
                player.Sks = Convert.ToDouble(ConfigurationManager.AppSettings["BASE_SKS"]);
            }
            else
            {
                player.Dex = Convert.ToDouble(ConfigurationManager.AppSettings["DEX"]);
                player.Crt = Convert.ToDouble(ConfigurationManager.AppSettings["CRT"]);
                player.Det = Convert.ToDouble(ConfigurationManager.AppSettings["DET"]);
                player.Sks = Convert.ToDouble(ConfigurationManager.AppSettings["SKS"]);
                player.Weapon = new Weapon
                {
                    WeaponDamage = Convert.ToDouble(ConfigurationManager.AppSettings["WD"]),
                    AutoAttack = Convert.ToDouble(ConfigurationManager.AppSettings["AA"]),
                    Delay = Convert.ToDouble(ConfigurationManager.AppSettings["AA_DELAY"])
                };
            }

            player.StatusEffects[StatusEffects.Huton] = (long)TimeSpan.FromSeconds(Convert.ToInt64(ConfigurationManager.AppSettings["HutonStartDuration"])).TotalMilliseconds;
            var kissOfTheWasp = Convert.ToBoolean(ConfigurationManager.AppSettings["KissOfTheWaspActive"]);

            if (kissOfTheWasp)
            {
                player.StatusEffects[StatusEffects.KissOfTheWasp] = long.MaxValue;
            }

            return player;
        }
    }
}
