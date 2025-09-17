namespace NewHarvestPatches
{
    internal static class FloorDropdownAdder
    {
        internal static void TryAddFloorDropdowns(bool moveNewHarvestWoodFloors, bool moveBaseWoodFloors, bool moveModWoodFloors)
        {
            StartStopwatch(nameof(FloorDropdownAdder), nameof(TryAddFloorDropdowns));
            try
            {
                AddDropdowns(moveNewHarvestWoodFloors, moveBaseWoodFloors, moveModWoodFloors);
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(FloorDropdownAdder), nameof(TryAddFloorDropdowns));
            }
        }

        private static List<ThingDef> GetBaseWoodDefs()
        {
            var woodList = new List<ThingDef>
            {
                ThingDefOf.WoodLog
            };
            return woodList;
        }

        private static List<ThingDef> GetNewHarvestWoodDefs()
        {
            var woodList = IndustrialResourceDefDictionary;
            if (woodList.NullOrEmpty())
                return [];

            // isWood = contains (not just startswith, for cases where another prefix is used (MO logs)) "VV_" and endswith "Wood" or "Lumber"
            return [.. woodList
                .Where(kvp => kvp.Value.isWood)
                .Select(kvp => kvp.Key)];
        }

        private static List<TerrainDef> FilterFloorList(
            this IEnumerable<TerrainDef> floors,
            bool moveNewHarvestWoodFloors,
            bool moveBaseWoodFloors,
            bool moveModWoodFloors)
        {
            if (floors.EnumerableNullOrEmpty())
                return [];

            var newHarvestWoodDefs = new HashSet<ThingDef>(GetNewHarvestWoodDefs());
            var baseWoodDefs = new HashSet<ThingDef>(GetBaseWoodDefs());

            bool IsNewHarvestFloor(TerrainDef td) =>
                td.costList.Any(cost => newHarvestWoodDefs.Contains(cost.thingDef));

            bool IsBaseWoodFloor(TerrainDef td) =>
                td.costList.Any(cost => baseWoodDefs.Contains(cost.thingDef));

            return [.. floors.Where(td =>
                (moveNewHarvestWoodFloors && IsNewHarvestFloor(td)) ||
                (moveBaseWoodFloors && IsBaseWoodFloor(td)) ||
                (moveModWoodFloors &&
                    !IsNewHarvestFloor(td) &&
                    !IsBaseWoodFloor(td))
            )];
        }

        private static void AddDropdowns(bool moveNewHarvestWoodFloors, bool moveBaseWoodFloors, bool moveModWoodFloors)
        {
            if (!moveNewHarvestWoodFloors && !moveBaseWoodFloors && !moveModWoodFloors)
                return;

            var floorsCategory = DesignationCategoryDefOf.Floors;
            if (floorsCategory == null) // Probably don't need
                return;
            var floors = DefDatabase<TerrainDef>.AllDefsListForReading
                .Where(td =>
                    td != null &&
                    !td.bridge &&
                    td.IsFloor &&
                    td.dominantStyleCategory == null &&
                    td.costList != null &&
                    td.costList.Count == 1 &&
                    (
                        (td.designationCategory?.label?.ContainsIgnoreCase("Floor") ?? false) ||
                        (td.designationCategory?.specialDesignatorClasses?.Contains(typeof(Designator_RemoveFloor)) == true)
                    ) &&
                    td.costList[0]?.thingDef?.stuffProps?.categories?.Any(
                        cat => cat.defName != null && cat.defName.ContainsIgnoreCase("Wood")) == true
                )
                .FilterFloorList(moveNewHarvestWoodFloors, moveBaseWoodFloors, moveModWoodFloors);

            if (floors.Count == 0)
            {
                ToLog("No floors found to add dropdowns to.", 1);
                return;
            }

            const string defPrefix = $"{ModName.Prefix.VV_NHCP_}FloorDropdown_";

            var dictionary = new Dictionary<ThingDef, (DesignatorDropdownGroupDef dropdown, List<TerrainDef> floors)>();
            foreach (var floor in floors)
            {
                var costDef = floor.costList[0].thingDef;
                if (!dictionary.TryGetValue(costDef, out var entry))
                {
                    var dropdown = new DesignatorDropdownGroupDef
                    {
                        defName = defPrefix + costDef.defName
                    };
                    entry = (dropdown, new List<TerrainDef>());
                    dictionary[costDef] = entry;
                }
                entry.floors.Add(floor);
            }

            if (dictionary.Count == 0)
                return;

            var designationCategories = new HashSet<DesignationCategoryDef>
            {
                floorsCategory
            };

            bool resolve = false;
            foreach (var kvp in dictionary)
            {
                if (kvp.Value.floors.Count <= 1)
                    continue; // Try to avoid putting single items in a dropdown


                float order = kvp.Value.floors.Min(floor => floor.uiOrder);

                foreach (var floor in kvp.Value.floors)
                {
                    var designationCategory = floor.designationCategory;
                    bool changeCategory = designationCategory != floorsCategory;
                    if (changeCategory)
                        floor.designationCategory = floorsCategory;

                    var oldDesignatorDropdown = floor.designatorDropdown;
                    floor.designatorDropdown = kvp.Value.dropdown;
                    floor.uiOrder = order;

                    designationCategories.Add(designationCategory); // For resolving after

                    resolve = true;

                    var oldDropdownName = (oldDesignatorDropdown != null && !string.IsNullOrWhiteSpace(oldDesignatorDropdown.defName))
                        ? oldDesignatorDropdown.defName
                        : "none";

                    ToLog($"Changed designatorDropdown on [{floor.defName}] from [{oldDropdownName}] to [{kvp.Value.dropdown.defName}].  designationCategory changed to {floorsCategory.defName} = {changeCategory}");
                }
            }

            if (resolve)
            {
                foreach (var category in designationCategories)
                {
                    category.ResolveReferences();
                }
            }
        }
    }
}