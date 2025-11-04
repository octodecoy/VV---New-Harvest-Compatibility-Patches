global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq;
global using System.Reflection;
global using System.Xml;
global using RimWorld;
global using UnityEngine;
global using Verse;
global using static NewHarvestPatches.NewHarvestPatchesModSettings;
global using static NewHarvestPatches.Utils.Logger;
global using static NewHarvestPatches.Utils.ModChecker;
global using static NewHarvestPatches.Utils.SettingChecker;
global using static NewHarvestPatches.Utils.VersionChecker;
global using static NewHarvestPatches.DefHelpers;
global using static NewHarvestPatches.Constants;
global using static UnityEngine.Color;

namespace NewHarvestPatches
{
    [StaticConstructorOnStartup]
    public static class Constructor
    {
        static Constructor()
        {
            var enabledSettings = EnabledSettings;

            var modVersions = NewHarvestVersions;
            if (!modVersions.NullOrEmpty()) // Dunno how
            {
                foreach (var modVersion in modVersions)
                {
                    var (version, translationKey) = modVersion.Value;
                    ToLog($"Detected New Harvest module: [{modVersion.Key} v{version}]");
                }
            }

            if (enabledSettings.Count() == 0)
            {
                // Nothing enabled, nothing to do past this point
                return;
            }

            foreach (var setting in enabledSettings)
            {
                ToLog($"Enabled setting: {setting}");
            }

            if (HasIndustrialModule)
            {
                if (enabledSettings.Any(s => s.StartsWith(Setting.Prefix.ColorChange_)))
                {
                    MaterialColorChanger.TryChangeMaterialColors();
                }

                if (Settings.AddMoreWoodFloors)
                {
                    BridgeDropdownAdder.TryAddBridgeDropdown();
                }

                if (!HasFernyFloorMenu)
                {
                    if (enabledSettings.Any(s => s.EndsWith(Setting.Suffix.ToDropdowns)))
                    {
                        FloorDropdownAdder.TryAddFloorDropdowns(
                        moveNewHarvestWoodFloors: Settings.NewHarvestWoodFloorsToDropdowns,
                        moveBaseWoodFloors: Settings.BaseWoodFloorsToDropdowns,
                        moveModWoodFloors: Settings.ModWoodFloorsToDropdowns);
                    }
                }

                if (HasIdeology)
                {
                    if (Settings.AddWoodDryads)
                    {
                        DryadUISorter.TrySortDryads();
                    }
                }

                if (ShowFuelSettings)
                {
                    if (enabledSettings.Any(s => s.StartsWith(Setting.Prefix.DisabledFuel_)))
                    {
                        DisallowFuelTypes.TryDisallowFuels();
                    }
                }

                if (enabledSettings.Any(s => s.StartsWith(Setting.Prefix.SetCommonality_)))
                {
                    StuffCommonalityChanger.TryChangeStuffCommonality();
                }
            }

            if (HasAnyTrees)
            {
                if (enabledSettings.Any(s => s.StartsWith(Setting.Prefix.NoFallColors_)))
                {
                    FallColorDisabler.TryDisableFallColors();
                }
            }

            CategoryLabelInfo.SetCategoryLabels();

            // LongEventHandler in the hope that other things are complete, such as base hay movements
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (enabledSettings.Any(s => s.EndsWith(Setting.Suffix.Category)))
                {
                    CategorySyncer.SyncAllFoods();
                }

                if (HasGardenModule)
                {
                    if (HasVanillaCookingExpanded && Settings.GrainsProduceVCEFlourSecondary)
                    {
                        FlourOutputFixer.TryFixFlourOutput();
                    }
                }

                LogInitTime();

                ClearCaches();
            });
        }
    }
}