namespace NewHarvestPatches
{
    internal static class BridgeDropdownAdder
    {
        internal static void TryAddBridgeDropdown()
        {
            StartStopwatch(nameof(BridgeDropdownAdder), nameof(TryAddBridgeDropdown));
            try
            {
                AddDropdown();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(BridgeDropdownAdder), nameof(TryAddBridgeDropdown));
            }
        }

        private static void AddDropdown()
        {
            var baseBridge = DefDatabase<TerrainDef>.GetNamedSilentFail("Bridge");
            if (baseBridge == null)
                return;

            var designationCategoryToUse = baseBridge.designationCategory;
            if (designationCategoryToUse == null)
                return;

            var modBridges = DefDatabase<TerrainDef>.AllDefsListForReading
                .Where(td => td?.defName?.StartsAndEndsWith(start: "VV_", end: "Bridge") == true && td.bridge)
                .ToList();

            if (modBridges.Count == 0)
                return;

            var bridgeDropdownToUse = baseBridge.designatorDropdown ??= new DesignatorDropdownGroupDef
            {
                defName = $"{ModName.Prefix.VV_NHCP_}BridgeDropdown"
            };

            var designationCategories = new HashSet<DesignationCategoryDef>
            {
                designationCategoryToUse
            };

            foreach (var bridge in modBridges)
            {
                var modBridgeDesignationCategory = bridge.designationCategory;
                if (modBridgeDesignationCategory == null || modBridgeDesignationCategory != designationCategoryToUse)
                    bridge.designationCategory = designationCategoryToUse;

                if (bridge.designatorDropdown == null || bridge.designatorDropdown != bridgeDropdownToUse)
                    bridge.designatorDropdown = bridgeDropdownToUse;

                if (modBridgeDesignationCategory != null)
                    designationCategories.Add(modBridgeDesignationCategory);
            }

            foreach (var category in designationCategories)
            {
                category.ResolveReferences();
            }
        }
    }
}