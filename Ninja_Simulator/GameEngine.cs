using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Ninja_Simulator.Configuration;
using Ninja_Simulator.Entities;
using Ninja_Simulator.Factories;
using Ninja_Simulator.Formulas;
using Ninja_Simulator.Parser;
using Ninja_Simulator.Skills;
using Newtonsoft.Json;

namespace Ninja_Simulator
{
    public class GameEngine
    {
        private static long _currentGameTime;
        private static readonly int EncounterLengthMs = (int)TimeSpan.FromSeconds(Convert.ToInt32(ConfigurationManager.AppSettings["EncounterLengthSeconds"])).TotalMilliseconds;
        private static readonly long GlobalAnimationLockMs = Convert.ToInt64(ConfigurationManager.AppSettings["GlobalAnimationLockMs"]);

        public static void Main()
        {
            while (true)
            {
                Console.WriteLine("Press 1 to run the simulation with configured stats, 2 to run the BIS solver with configured inventory, 3 to regenerate skill speed rankings (recommended after changing the rotation), 9 to exit.");
                try
                {
                    switch (Convert.ToInt32(Console.ReadKey().KeyChar.ToString()))
                    {
                        case 1:
                            RunSimulation(true);
                            break;
                        case 2:
                            RunBestInSlotSolver();
                            break;
                        case 3:
                            RegenerateSkillSpeedRanks();
                            break;
                        case 9:
                            Environment.Exit(0);
                            break;
                        default:
                            Console.WriteLine("\nInvalid command. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occured: {ex}.");
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static void ResetValues()
        {
            _currentGameTime = 0;
        }

        private static void RegenerateSkillSpeedRanks()
        {
            var minSkillSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["MIN_SKS"]);
            var maxSkillSpeed = Convert.ToInt32(ConfigurationManager.AppSettings["MAX_SKS"]);
            Console.WriteLine($"Regenerating skill speed rankings ... values from { minSkillSpeed } to { maxSkillSpeed }. If this range is not the full range desired, please enter the full range in the program configuration.");

            var skillSpeedDps = new Dictionary<int, double>();

            for (var i = minSkillSpeed; i <= maxSkillSpeed; i++)
            {
                var player = PlayerFactory.CreatePlayer();
                player.Sks = i;
                var dps = RunSimulation(false, player);
                Console.WriteLine($"Skill Speed: { i }, DPS: { dps }");
                skillSpeedDps.Add(i, dps);
            }

            using (var filestream = File.OpenWrite(FormulaLibrary.GetSkillSpeedRanksFilePath()))
            using (var writer = new StreamWriter(filestream))
            {
                writer.Write(JsonConvert.SerializeObject(skillSpeedDps));
            }
        }

        private static void RunBestInSlotSolver()
        {
            var numIterations = 0;
            var accFilter = 0;
            var sksFilter = 0;
            var strictFilter = 0;
            var primaryStatFilter = 0;
            var secondaryStatFilter = 0;

            var recordedRuns = new List<RecordedDPS>();

            // Read and filter inventory.
            var weaponItems = FilterWeapons(Inventory.Config.MainHand.Cast<InventorySection.MainHandElementCollection.ItemElement>().ToList());
            var headItems = FilterItems<InventorySection.HeadElementCollection.ItemElement>(Inventory.Config.Head.Cast<InventorySection.HeadElementCollection.ItemElement>().ToList());
            var bodyItems = FilterItems<InventorySection.BodyElementCollection.ItemElement>(Inventory.Config.Body.Cast<InventorySection.BodyElementCollection.ItemElement>().ToList());
            var handItems = FilterItems<InventorySection.HandsElementCollection.ItemElement>(Inventory.Config.Hands.Cast<InventorySection.HandsElementCollection.ItemElement>().ToList());
            var waistItems = FilterItems<InventorySection.WaistElementCollection.ItemElement>(Inventory.Config.Waist.Cast<InventorySection.WaistElementCollection.ItemElement>().ToList());
            var legItems = FilterItems<InventorySection.LegsElementCollection.ItemElement>(Inventory.Config.Legs.Cast<InventorySection.LegsElementCollection.ItemElement>().ToList());
            var feetItems = FilterItems<InventorySection.FeetElementCollection.ItemElement>(Inventory.Config.Feet.Cast<InventorySection.FeetElementCollection.ItemElement>().ToList());
            var neckItems = FilterItems<InventorySection.NeckElementCollection.ItemElement>(Inventory.Config.Neck.Cast<InventorySection.NeckElementCollection.ItemElement>().ToList());
            var earItems = FilterItems<InventorySection.EarsElementCollection.ItemElement>(Inventory.Config.Ears.Cast<InventorySection.EarsElementCollection.ItemElement>().ToList());
            var wristItems = FilterItems<InventorySection.WristsElementCollection.ItemElement>(Inventory.Config.Wrists.Cast<InventorySection.WristsElementCollection.ItemElement>().ToList());

            // We don't filter rings or food because they're a little different than just stat sticks. Rings are unique and food boost stats by percentages.

            var leftRingItems = Inventory.Config.LeftRing.Cast<InventorySection.LeftRingElementCollection.ItemElement>().ToList();
            var rightRingItems = Inventory.Config.RightRing.Cast<InventorySection.RightRingElementCollection.ItemElement>().ToList();
            var foodItems = Inventory.Config.Food.Cast<InventorySection.FoodElementCollection.ItemElement>().ToList();

            // for all combinations
            foreach (var weapon in weaponItems)
            {
                foreach (var head in headItems)
                {
                    foreach (var body in bodyItems)
                    {
                        foreach (var hands in handItems)
                        {
                            foreach (var waist in waistItems)
                            {
                                foreach (var legs in legItems)
                                {
                                    foreach (var feet in feetItems)
                                    {
                                        foreach (var neck in neckItems)
                                        {
                                            foreach (var ears in earItems)
                                            {
                                                foreach (var wrists in wristItems)
                                                {
                                                    foreach (var leftRing in leftRingItems)
                                                    {
                                                        foreach (var rightRing in rightRingItems)
                                                        {
                                                            // rings are unique
                                                            if (rightRing.Name.Equals(leftRing.Name,
                                                                StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                continue;
                                                            }
                                                            foreach (var food in foodItems)
                                                            {
                                                                var player = PlayerFactory.CreatePlayer(true);
                                                                // add weapon
                                                                player.Weapon = new Weapon
                                                                {
                                                                    AutoAttack = weapon.Aa,
                                                                    WeaponDamage = weapon.Wd,
                                                                    Delay = weapon.Delay
                                                                };

                                                                // add stats
                                                                player.Dex += weapon.Dex + head.Dex + body.Dex +
                                                                              hands.Dex + waist.Dex + legs.Dex +
                                                                              feet.Dex + neck.Dex + ears.Dex +
                                                                              wrists.Dex + leftRing.Dex + rightRing.Dex;
                                                                player.Acc += weapon.Acc + head.Acc + body.Acc +
                                                                              hands.Acc + waist.Acc + legs.Acc +
                                                                              feet.Acc + neck.Acc + ears.Acc +
                                                                              wrists.Acc + leftRing.Acc + rightRing.Acc;
                                                                player.Crt += weapon.Crt + head.Crt + body.Crt +
                                                                              hands.Crt + waist.Crt + legs.Crt +
                                                                              feet.Crt + neck.Crt + ears.Crt +
                                                                              wrists.Crt + leftRing.Crt + rightRing.Crt;
                                                                player.Det += weapon.Det + head.Det + body.Det +
                                                                              hands.Det + waist.Det + legs.Det +
                                                                              feet.Det + neck.Det + ears.Det +
                                                                              wrists.Det + leftRing.Det + rightRing.Det;
                                                                player.Sks += weapon.Sks + head.Sks + body.Sks +
                                                                              hands.Sks + waist.Sks + legs.Sks +
                                                                              feet.Sks + neck.Sks + ears.Sks +
                                                                              wrists.Sks + leftRing.Sks + rightRing.Sks;

                                                                // add food bonus
                                                                var accBonus = (int)(player.Acc * food.AccModifier) - (int)player.Acc;
                                                                player.Acc += accBonus >= food.MaxAccBonus ? food.MaxAccBonus : accBonus;

                                                                var crtBonus = (int)(player.Crt * food.CrtModifier) - (int)player.Crt;
                                                                player.Crt += crtBonus >= food.MaxCrtBonus ? food.MaxCrtBonus : crtBonus;

                                                                var detBonus = (int)(player.Det * food.DetModifier) - (int)player.Det;
                                                                player.Det += detBonus >= food.MaxDetBonus ? food.MaxDetBonus : detBonus;

                                                                var sksBonus = (int)(player.Sks * food.SksModifier) - (int)player.Sks;
                                                                player.Sks += sksBonus >= food.MaxSksBonus ? food.MaxSksBonus : sksBonus;

                                                                // add party bonus
                                                                player.Dex = (int)(player.Dex * 1.03);

                                                                // run heuristics to pre-filter worse combinations
                                                                if (player.Acc < Convert.ToInt32(ConfigurationManager.AppSettings["MIN_ACC"]))
                                                                {
                                                                    accFilter++;
                                                                    continue;
                                                                }
                                                                if (player.Sks < Convert.ToInt32(ConfigurationManager.AppSettings["MIN_SKS"]) ||
                                                                    player.Sks > Convert.ToInt32(ConfigurationManager.AppSettings["MAX_SKS"]))
                                                                {
                                                                    sksFilter++;
                                                                    continue;
                                                                }

                                                                try
                                                                {
                                                                    if (recordedRuns.Count > 0)
                                                                    {
                                                                        var skip = false;
                                                                        foreach (var recordedRun in recordedRuns)
                                                                        {
                                                                            var recordedPlayer = recordedRun.Player;

                                                                            if (player.Weapon.WeaponDamage <= recordedPlayer.Weapon.WeaponDamage &&
                                                                                player.Dex <= recordedPlayer.Dex &&
                                                                                player.Crt <= recordedPlayer.Crt &&
                                                                                player.Det <= recordedPlayer.Det &&
                                                                                FormulaLibrary.GetSkillSpeedRank(player.Sks) <=
                                                                                FormulaLibrary.GetSkillSpeedRank(recordedPlayer.Sks))
                                                                            {
                                                                                strictFilter++;
                                                                                skip = true;
                                                                                break;
                                                                            }

                                                                            if (player.Weapon.WeaponDamage <= recordedPlayer.Weapon.WeaponDamage &&
                                                                                player.Dex <= recordedPlayer.Dex &&
                                                                                player.Crt >= recordedPlayer.Crt &&
                                                                                player.Det >= recordedPlayer.Det &&
                                                                                FormulaLibrary.GetSkillSpeedRank(player.Sks) <=
                                                                                FormulaLibrary.GetSkillSpeedRank(recordedPlayer.Sks))
                                                                            {
                                                                                var crtGain = player.Crt - recordedPlayer.Crt;
                                                                                var detGain = player.Det - recordedPlayer.Det;
                                                                                var secondaryStatGain = crtGain * 1.5 + detGain;

                                                                                var strLoss = recordedPlayer.Dex - player.Dex;
                                                                                var wdLoss = recordedPlayer.Weapon.WeaponDamage - player.Weapon.WeaponDamage;
                                                                                var primaryStatLoss = strLoss + 5 * wdLoss;

                                                                                if (secondaryStatGain < primaryStatLoss)
                                                                                {
                                                                                    primaryStatFilter++;
                                                                                    skip = true;
                                                                                    break;
                                                                                }
                                                                            }

                                                                            if (player.Weapon.WeaponDamage <= recordedPlayer.Weapon.WeaponDamage &&
                                                                                player.Dex <= recordedPlayer.Dex &&
                                                                                FormulaLibrary.GetSkillSpeedRank(player.Sks) <=
                                                                                FormulaLibrary.GetSkillSpeedRank(recordedPlayer.Sks))
                                                                            {
                                                                                if (player.Crt > recordedPlayer.Crt && player.Det < recordedPlayer.Det)
                                                                                {
                                                                                    var critGain = player.Crt - recordedPlayer.Crt;
                                                                                    var detLoss = recordedPlayer.Det - player.Det;
                                                                                    if (critGain * 1.75 <= detLoss)
                                                                                    {
                                                                                        secondaryStatFilter++;
                                                                                        skip = true;
                                                                                        break;
                                                                                    }
                                                                                }
                                                                                if (player.Crt < recordedPlayer.Crt && player.Det > recordedPlayer.Det)
                                                                                {
                                                                                    var detGain = player.Det - recordedPlayer.Det;
                                                                                    var critLoss = recordedPlayer.Crt - player.Crt;
                                                                                    if (detGain * 1.25 <= critLoss)
                                                                                    {
                                                                                        secondaryStatFilter++;
                                                                                        skip = true;
                                                                                        break;
                                                                                    }
                                                                                }
                                                                            }
                                                                        }

                                                                        if (skip)
                                                                        {
                                                                            continue;
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    continue;
                                                                }


                                                                // run the sim
                                                                try
                                                                {
                                                                    numIterations++;
                                                                    var dps = RunSimulation(false, player);
                                                                    recordedRuns.Add(new RecordedDPS
                                                                    {
                                                                        Player = player,
                                                                        DPS = dps,
                                                                        EquipmentNames = new List<string>
                                                                        {
                                                                            weapon.Name, head.Name, body.Name, hands.Name, waist.Name,
                                                                            legs.Name, feet.Name, neck.Name, ears.Name, wrists.Name,
                                                                            leftRing.Name, rightRing.Name, food.Name
                                                                        }
                                                                    });

                                                                    Console.WriteLine($"Iteration {numIterations} - DPS is: {dps}");
                                                                }
                                                                catch (Exception)
                                                                {
                                                                    // suppress errors due to SKS
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var counter = 1;
            foreach (var run in recordedRuns.OrderByDescending(r => r.DPS))
            {
                Console.WriteLine($"--Number {counter++}");
                Console.WriteLine("DPS: " + run.DPS);
                Console.WriteLine("Player Equipment:");
                run.EquipmentNames.ForEach(Console.WriteLine);
                Console.WriteLine("Player stats:");
                Console.WriteLine($"Dex: {run.Player.Dex}");
                Console.WriteLine($"Acc: {run.Player.Acc}");
                Console.WriteLine($"Crt: {run.Player.Crt}");
                Console.WriteLine($"Det: {run.Player.Det}");
                Console.WriteLine($"Sks: {run.Player.Sks}");

                if (counter > 250)
                {
                    break;
                }
            }

            Console.WriteLine("Diagnostics: ");
            Console.WriteLine($"AccFilter: {accFilter}");
            Console.WriteLine($"SksFilter: {sksFilter}");
            Console.WriteLine($"StrictFilter: {strictFilter}");
            Console.WriteLine($"PrimaryStatFilter: {primaryStatFilter}");
            Console.WriteLine($"SecondaryStatFilter: {secondaryStatFilter}");
        }

        private static double RunSimulation(bool verbose, Player providedPlayer = null)
        {
            ResetValues();
            var actors = new List<Actor>();
            var player = providedPlayer ?? PlayerFactory.CreatePlayer();
            var strikingDummy = StrikingDummyFactory.CreateStrikingDummy();
            actors.Add(player);
            actors.Add(strikingDummy);

            RotationParser.LoadRotation((int)player.Sks);
            var selectedAbility = RotationParser.SelectFirstAbility(strikingDummy, verbose);

            while (_currentGameTime < EncounterLengthMs)
            {
                strikingDummy.DamageTaken += player.AutoAttack(strikingDummy);

                if (selectedAbility is WeaponSkills)
                {
                    var skillDamage = player.Attack(strikingDummy, (WeaponSkills)selectedAbility);
                    if (skillDamage > 0)
                    {
                        if (verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(
                                $"T: {FormatTimespan(TimeSpan.FromMilliseconds(GetCurrentGameTime()))} | Used {selectedAbility} for {skillDamage} damage!");
                        }
                        strikingDummy.DamageTaken += skillDamage;
                        selectedAbility = RotationParser.SelectNextAbility(strikingDummy, false, verbose);
                        IncrementTimers(actors, verbose);
                        continue;
                    }
                }
                if (selectedAbility is Spells)
                {
                    if (SpellLibrary.IsDamageSpell((Spells)selectedAbility))
                    {
                        var spellDamage = player.UseDamageSpell(strikingDummy, (Spells)selectedAbility, verbose);
                        if (spellDamage > 0)
                        {
                            if (verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Used {selectedAbility} for {spellDamage} damage!");
                            }

                            strikingDummy.DamageTaken += spellDamage;
                            selectedAbility = RotationParser.SelectNextAbility(strikingDummy, false, verbose);
                            IncrementTimers(actors, verbose);
                            continue;
                        }
                    }
                    if (SpellLibrary.IsBuffSpell((Spells)selectedAbility))
                    {
                        var castSuccessfully = player.UseBuffSpell(strikingDummy, (Spells)selectedAbility, verbose);
                        if (castSuccessfully)
                        {
                            if (verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Used {selectedAbility}!");
                            }

                            selectedAbility = RotationParser.SelectNextAbility(strikingDummy, false, verbose);
                            IncrementTimers(actors, verbose);
                            continue;
                        }
                    }
                }

                IncrementTimers(actors, verbose);
            }

            if (verbose)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Console.WriteLine($"Average Encounter DPS: {strikingDummy.DamageTaken / TimeSpan.FromMilliseconds(EncounterLengthMs).TotalSeconds}");
            }

            return strikingDummy.DamageTaken / TimeSpan.FromMilliseconds(EncounterLengthMs).TotalSeconds;
        }

        private static void IncrementTimers(IEnumerable<Actor> actors, bool verbose)
        {
            _currentGameTime++;

            foreach (var actor in actors)
            {
                var player = actor as Player;
                if (player != null)
                {
                    if (player.AutoAttackDuration > 0)
                    {
                        player.AutoAttackDuration--;
                    }
                    if (player.GcdDuration > 0)
                    {
                        player.GcdDuration--;
                    }
                }

                actor.DecrementDamageOverTimeDuration(verbose);
                actor.DecrementQueuedEffectDuration(verbose);
                actor.DecrementStatusEffectDuration(verbose);
                actor.DecrementCooldownDuration(verbose);
            }
        }

        private static IList<T> FilterItems<T>(IEnumerable<Item> items) where T : Item
        {
            var filteredList = new List<T>();

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var item in items)
            {
                if (filteredList.Any(i => i.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                if (filteredList.Any(i => i.Dex >= item.Dex && i.Crt >= item.Crt && i.Det >= item.Det && i.Sks == item.Sks))
                {
                    continue;
                }

                filteredList.Add((T)item);
            }

            return filteredList;
        }

        private static IList<InventorySection.MainHandElementCollection.ItemElement> FilterWeapons(IEnumerable<InventorySection.MainHandElementCollection.ItemElement> items)
        {
            var filteredList = new List<InventorySection.MainHandElementCollection.ItemElement>();

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var item in items)
            {
                if (filteredList.Any(i => i.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue;
                }

                if (filteredList.Any(i => i.Wd >= item.Wd && i.Aa >= item.Aa && i.Delay <= item.Delay &&
                    i.Dex >= item.Dex && i.Crt >= item.Crt && i.Det >= item.Det && i.Sks == item.Sks))
                {
                    continue;
                }

                filteredList.Add(item);
            }

            return filteredList;
        }

        private static string FormatTimespan(TimeSpan timespan)
        {
            var minutes = timespan.Minutes.ToString();
            if (timespan.Minutes < 10)
            {
                minutes = $"0{minutes}";
            }

            var seconds = timespan.Seconds.ToString();
            if (timespan.Seconds < 10)
            {
                seconds = $"0{seconds}";
            }

            var milliseconds = timespan.Milliseconds.ToString();
            if (timespan.Milliseconds < 10)
            {
                milliseconds = $"0{milliseconds}";
            }
            if (milliseconds.Length > 2)
            {
                milliseconds = milliseconds.Substring(0, 2);
            }

            return $"{minutes}:{seconds}:{milliseconds}";
        }

        public static long GetCurrentGameTime()
        {
            return _currentGameTime;
        }

        public static string GetFormattedGameTime()
        {
            return $"T: {FormatTimespan(TimeSpan.FromMilliseconds(GetCurrentGameTime()))}";
        }

        public static long GetGlobalAnimationLockDurationMs()
        {
            return GlobalAnimationLockMs;
        }
    }
}
