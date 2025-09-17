namespace NewHarvestPatches
{
    internal static class StuffCommonalityChanger
    {
        /// <summary>
        /// Change commonality of stuff.  If VEF is installed, we insert our defs that will be using VEF.Things.StuffExtension into VEF's cache.
        /// </summary>
        internal static void TryChangeStuffCommonality()
        {
            StartStopwatch(nameof(StuffCommonalityChanger), nameof(TryChangeStuffCommonality));
            try
            {
                // Have to keep the methods separated, due to using HarmonyLib.AccessTools & VEF.Things.StuffExtension
                if (ShowVEFCommonalitySettings)
                    ChangeStuffCommonality_VEF();
                else
                    ChangeStuffCommonality_Standard();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(StuffCommonalityChanger), nameof(TryChangeStuffCommonality));
            }
        }


        private static ThingDef[] GetEnabledDefs()
        {
            //var enabledDefNames = EnabledSettings
            //    .Where(s => s.StartsWith(Setting.Prefix.SetCommonality_))
            //    .Select(s => s.Substring(Setting.Prefix.SetCommonality_.Length))
            //    .ToList();

            var enabledDefNames = ExtractNamesFromEnabledSettings(Setting.Prefix.SetCommonality_);

            return !enabledDefNames.NullOrEmpty() ? [.. GetDefsOfTypeByDefNames<ThingDef>(order: false, defNames: [.. enabledDefNames])] : [];
        }

        private static void ChangeStuffCommonality_Standard()
        {
            var enabledDefs = GetEnabledDefs();
            if (enabledDefs.NullOrEmpty())
                return;

            var stuffDefDictionary = Settings.StuffCommonality;
            if (stuffDefDictionary.NullOrEmpty())
                return;

            var dictionary = new Dictionary<ThingDef, CommonalityInfo>();
            foreach (var def in enabledDefs)
            {
                if (stuffDefDictionary.TryGetValue(def.defName, out var info))
                    dictionary[def] = info;
            }

            if (dictionary.NullOrEmpty())
                return;

            foreach (var kvp in dictionary)
            {
                var def = kvp.Key;

                var info = dictionary[def];

                var oldCommonality = def.stuffProps?.commonality;
                if (oldCommonality == null || oldCommonality == info.CoreCommonality)
                    continue; // No change

                def.stuffProps.commonality = info.CoreCommonality;
                ToLog(
                    $"Set commonality for {def.defName} -> " +
                    $"Default Commonality={kvp.Value.DefaultCommonality}, " +
                    $"Old Commonality={oldCommonality}, " +
                    $"New Commonality={def.stuffProps.commonality}");
            }
        }

        private static void ChangeStuffCommonality_VEF()
        {
            var stuffDefDictionary = Settings.StuffCommonality;
            if (stuffDefDictionary.NullOrEmpty())
                return;

            var enabledDefs = GetEnabledDefs();
            if (enabledDefs.NullOrEmpty())
                return;

            var dictionary = new Dictionary<ThingDef, CommonalityInfo>();
            foreach (var def in enabledDefs)
            {
                if (stuffDefDictionary.TryGetValue(def.defName, out var info))
                    dictionary[def] = info;
            }

            if (dictionary.NullOrEmpty())
                return;

            const string vefMethod = "VanillaExpandedFramework_ThingStuffPair_Commonality_Patch";
            var patchType = HarmonyLib.AccessTools.TypeByName(vefMethod);
            if (patchType == null)
            {
                ToLog($"Patch type [{vefMethod}] not found. Stuff commonality changes aborted.", 1);
                return;
            }

            const string vefField = "cachedExtension";
            var cacheField = patchType.GetField(vefField, BindingFlags.Static | BindingFlags.NonPublic);
            if (cacheField == null)
            {
                ToLog($"Field [{vefField}] not found in patch type [{vefMethod}]. Stuff commonality changes aborted.", 1);
                return;
            }

            var cacheObj = cacheField.GetValue(null);
            if (cacheObj is not Dictionary<ThingDef, VEF.Things.StuffExtension> cache)
            {
                if (cacheObj == null)
                    ToLog($"Cache field [{vefField}] in patch type [{vefMethod}] is null. Stuff commonality changes aborted.", 1);
                else
                    ToLog($"Cache field [{vefField}] in patch type [{vefMethod}] is not a Dictionary<ThingDef, VEF.Things.StuffExtension>. Stuff commonality changes aborted.", 1);
                return;
            }

            foreach (var kvp in dictionary)
            {
                var def = kvp.Key;
                var info = dictionary[def];

                // Make factor 1x if offset is >0, otherwise 0 - this way our offsets take the place of the base commonality
                var structureFactor = 0f;
                if (info.StructureOffset > 0f)
                    structureFactor = 1f;

                var weaponFactor = 0f;
                if (info.WeaponOffset > 0f)
                    weaponFactor = 1f;

                var apparelFactor = 0f;
                if (info.ApparelOffset > 0f)
                    apparelFactor = 1f;

                var stuffExt = new VEF.Things.StuffExtension
                {
                    structureGenerationCommonalityOffset = info.StructureOffset,
                    weaponGenerationCommonalityOffset = info.WeaponOffset,
                    apparelGenerationCommonalityOffset = info.ApparelOffset,
                    structureGenerationCommonalityFactor = structureFactor,
                    weaponGenerationCommonalityFactor = weaponFactor,
                    apparelGenerationCommonalityFactor = apparelFactor
                };

                // I dunno, why not
                if (stuffExt == null)
                    continue;

                def.stuffProps.commonality = 0f; // Set commonality to 0 so that the StuffExtension controls commonality

                def.modExtensions ??= []; // Add modExtension if it doesn't exist, which it shouldn't
                def.modExtensions.RemoveAll(x => x is VEF.Things.StuffExtension); // Remove any StuffExtension already on the def, which there shouldn't be
                def.modExtensions.Add(stuffExt);

                stuffExt.ResolveReferences(def); // Resolve the new StuffExtension

                cache[def] = stuffExt; // Update VEF cache with the new StuffExtension

                ToLog(
                    $"Set commonality for {def.defName} -> " +
                    $"Default Commonality={kvp.Value.DefaultCommonality}, " +
                    $"Structure Offset={stuffExt.structureGenerationCommonalityOffset}, " +
                    $"Weapon Offset={stuffExt.weaponGenerationCommonalityOffset}, " +
                    $"Apparel Offset={stuffExt.apparelGenerationCommonalityOffset}, " +
                    $"Structure Factor={stuffExt.structureGenerationCommonalityFactor}, " +
                    $"Weapon Factor={stuffExt.weaponGenerationCommonalityFactor}, " +
                    $"Apparel Factor={stuffExt.apparelGenerationCommonalityFactor}");
            }
        }
    }
}