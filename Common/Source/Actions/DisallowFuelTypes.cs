namespace NewHarvestPatches
{
    internal static class DisallowFuelTypes
    {
        /// <summary>
        /// Disallow specified fuel types on all refuelable buildings.  
        /// Reflection after checking base CompProperties_Refuelable so we can hopefully get any mod added fuelFilter using comps too.
        /// </summary>
        /// 
        internal static void TryDisallowFuels()
        {
            StartStopwatch(nameof(DisallowFuelTypes), nameof(TryDisallowFuels));
            try
            {
                DisallowFuels();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(DisallowFuelTypes), nameof(TryDisallowFuels));
            }
        }

        private static void DisallowFuels()
        {
            //var disabledFuelDefNames = EnabledSettings
            //    .Where(s => s.StartsWith(Setting.Prefix.DisabledFuel_))
            //    .Select(s => s.Substring(Setting.Prefix.DisabledFuel_.Length))
            //    .ToList();

            var disabledDefNames = ExtractNamesFromEnabledSettings(Setting.Prefix.DisabledFuel_);
            if (disabledDefNames.NullOrEmpty())
                return;

            var fuelDefs = GetDefsOfTypeByDefNames<ThingDef>(defNames: [.. disabledDefNames]).ToHashSet();
            if (fuelDefs.NullOrEmpty())
                return;

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.building == null) // Might need to rethink
                    continue;

                if (def.comps.NullOrEmpty())
                    continue;

                var compPropertiesRefuelable = def.GetCompProperties<CompProperties_Refuelable>();
                if (compPropertiesRefuelable != null)
                {
                    var fuelFilter = compPropertiesRefuelable.fuelFilter;
                    if (fuelFilter != null)
                    {
                        var allowedThingDefs = fuelFilter.AllowedThingDefs;
                        if (!allowedThingDefs.EnumerableNullOrEmpty())
                        {
                            if (allowedThingDefs.Any(fuelDefs.Contains))
                            {
                                foreach (var fuelDef in fuelDefs)
                                {
                                    if (fuelFilter.Allows(fuelDef))
                                    {
                                        fuelFilter.SetAllow(fuelDef, false);
                                        ToLog($"Disallowed [{fuelDef.defName}] on [{def.defName}]");
                                    }
                                }
                            }
                        }
                    }
                    continue; // Continue to next def, no need to check comps further if we found the base refuelable comp, at least thats my thinking
                }

                // Check all comp properties for a 'fuelFilter' field/property
                foreach (var comp in def.comps)
                {
                    var compType = comp.GetType();
                    if (compType == null)
                        continue;

                    var fuelFilterField = compType.GetField("fuelFilter");
                    if (fuelFilterField == null)
                        continue;

                    if (fuelFilterField.GetValue(comp) is not ThingFilter fuelFilter)
                        continue;

                    var allowedThingDefsProp = typeof(ThingFilter).GetProperty("AllowedThingDefs");
                    if (allowedThingDefsProp == null)
                        continue;

                    if (allowedThingDefsProp.GetValue(fuelFilter) is not IEnumerable<ThingDef> allowedThingDefs)
                        continue;

                    if (!allowedThingDefs.Any(fuelDefs.Contains))
                        continue;

                    foreach (var fuelDef in fuelDefs)
                    {
                        if (allowedThingDefs.Contains(fuelDef))
                        {
                            fuelFilter.SetAllow(fuelDef, false);
                            ToLog($"Disallowed [{fuelDef.defName}] on [{def.defName}] via reflection");
                        }
                    }
                }
            }
        }
    }
}