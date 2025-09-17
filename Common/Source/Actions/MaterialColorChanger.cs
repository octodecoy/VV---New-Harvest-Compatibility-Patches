namespace NewHarvestPatches
{
    internal static class MaterialColorChanger
    {
        /// <summary>
        /// Changes colors of ThingDefs and TerrainDefs built out of them.
        /// </summary>

        private static readonly MethodInfo _resolveThingDefIconMethod =
            typeof(ThingDef).GetMethod("ResolveIcon", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo _resolveTerrainDefIconMethod =
            typeof(TerrainDef).GetMethod("ResolveIcon", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void TryChangeMaterialColors()
        {
            StartStopwatch(nameof(MaterialColorChanger), nameof(TryChangeMaterialColors));
            try
            {
                ChangeAllColors();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(MaterialColorChanger), nameof(TryChangeMaterialColors));
            }
        }

        private static void ChangeAllColors()
        {
            var dictionary = Settings.MaterialColors;
            if (dictionary.NullOrEmpty())
                return;

            //var enabledDefNames = EnabledSettings
            //    .Where(s => s.StartsWith(Setting.Prefix.ColorChange_))
            //    .Select(s => s.Substring(Setting.Prefix.ColorChange_.Length))
            //    .ToList();

            var enabledDefNames = ExtractNamesFromEnabledSettings(Setting.Prefix.ColorChange_);
            if (enabledDefNames.NullOrEmpty())
                return;

            foreach (var defName in enabledDefNames)
            {
                if (!dictionary.TryGetValue(defName, out var colorInfo))
                    continue;

                var (stuffSet, thingSet) = ChangeDefColors(defName, colorInfo.NewStuffColor, colorInfo.NewThingColor, colorInfo.DoStuff, colorInfo.DoThing);
                ToLog(
                    $"[{defName} color change] " +
                    $"DoStuff({colorInfo.DoStuff}): set={stuffSet}, color={(stuffSet ? colorInfo.NewStuffColor : "none")} | " +
                    $"DoThing({colorInfo.DoThing}): set={thingSet}, color={(thingSet ? colorInfo.NewThingColor : "none")}");
            }
        }

        public static (bool stuffSet, bool thingSet) ChangeDefColors(string defName, Color newStuffColor, Color newThingColor, bool doStuff = false, bool doThing = false)
        {
            if (!doStuff && !doThing)
            {
                return (false, false);
            }

            if (_resolveThingDefIconMethod == null && doThing)
            {
                return (false, false);
            }

            if (newStuffColor == null || string.IsNullOrWhiteSpace(defName))
            {
                return (false, false);
            }

            if (DefDatabase<ThingDef>.GetNamedSilentFail(defName) is not ThingDef def)
            {
                return (false, false);
            }

            if (doStuff)
            {
                if (def.stuffProps == null)
                {
                    return (false, false);
                }
                def.stuffProps.color = newStuffColor; // Change stuff color

                ChangeTerrainDefColors(defName, newStuffColor);
                //ChangeBuildingColors(defName, newStuffColor);
            }

            // Change thing color
            if (!doThing)
            {
                return (doStuff, false);
            }

            if (def.graphicData == null)
            {
                return (doStuff, false);
            }
            if (Equals(newThingColor, def.graphicData.color))
            {
                return (doStuff, true);
            }
            def.graphicData.color = newThingColor;
            def.graphic = def.graphicData.Graphic; // Regenerate graphic
            _resolveThingDefIconMethod.Invoke(def, null); // ResolveIcon

            return (doStuff, true);
        }

        private static void ChangeTerrainDefColors(string defName, Color newStuffColor)
        {
            if (_resolveTerrainDefIconMethod == null)
                return;

            var terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading
                .Where(td => td?.costList != null &&
                             td.costList.Count == 1 &&
                             td.costList[0]?.thingDef?.defName == defName &&
                             !Equals(td.color, newStuffColor));

            if (terrainDefs.Count() == 0)
            {
                return;
            }

            foreach (var terrain in terrainDefs)
            {
                terrain.color = newStuffColor;
                terrain.graphic = GraphicDatabase.Get<Graphic_Terrain>(
                    terrain.texturePath,
                    terrain.Shader,
                    Vector2.one,
                    terrain.DrawColor,
                    2000 + terrain.renderPrecedence
                );
                _resolveTerrainDefIconMethod.Invoke(terrain, null);
            }
        }

        //private static void ChangeBuildingColors(string defName, Color newStuffColor)
        //{
        //    var buildingDefs = DefDatabase<ThingDef>.AllDefsListForReading
        //        .Where(td => td?.building != null &&
        //                     td.costList != null &&
        //                     td.costList.Any(td => td.thingDef?.defName == defName) &&
        //                     td.graphicData != null &&
        //                     !Equals(td.graphicData.color, newStuffColor));

        //    if (buildingDefs.Count() == 0)
        //    {
        //        return;
        //    }

        //    foreach (var building in buildingDefs)
        //    {
        //        building.graphicData.color = newStuffColor;
        //        building.graphic = building.graphicData.Graphic; // Regenerate graphic
        //        _resolveThingDefIconMethod.Invoke(building, null); // ResolveIcon
        //    }
        //}
    }
}