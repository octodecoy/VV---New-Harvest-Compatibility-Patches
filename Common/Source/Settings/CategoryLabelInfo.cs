namespace NewHarvestPatches
{
    public class CategoryLabelInfo : IExposable
    {
        public string OriginalCategoryLabel = ""; // No need to scribe since it could change between sessions so just update it on load and use to revert
        public string CurrentCategoryLabel = "";
        public void ExposeData()
        {
            Scribe_Values.Look(ref CurrentCategoryLabel, nameof(CurrentCategoryLabel), "", false);
        }

        internal static void CacheDefaultCategoryLabelInfo(ref Dictionary<string, CategoryLabelInfo> categoryLabelCache)
        {
            categoryLabelCache ??= [];
            var categories = ThingCategoryUtility.GetThingCategoryDefs();
            foreach (var category in categories)
            {
                if (!categoryLabelCache.TryGetValue(category.defName, out var categoryLabelInfo))
                {
                    categoryLabelInfo = new CategoryLabelInfo
                    {
                        OriginalCategoryLabel = category.label,
                        CurrentCategoryLabel = category.label
                    };
                    categoryLabelCache[category.defName] = categoryLabelInfo;
                }
                else
                {
                    // Update original label in case it changed due to mod updates
                    categoryLabelInfo.OriginalCategoryLabel = category.label;
                }
            }
        }

        internal static void UpdateCategoryLabelInfo(ThingCategoryDef category, string newLabel)
        {
            if (Settings.CategoryLabelCache.TryGetValue(category.defName, out var existingEntry))
            {
                existingEntry.CurrentCategoryLabel = newLabel;
            }
            else
            {
                // Create a new CategoryLabelInfo
                var newLabelInfo = new CategoryLabelInfo
                {
                    OriginalCategoryLabel = category.label,
                    CurrentCategoryLabel = newLabel
                };
                Settings.CategoryLabelCache[category.defName] = newLabelInfo;
            }
        }

        internal static void SetCategoryLabels()
        {
            if (Settings.CategoryLabelCache.NullOrEmpty())
                return;

            var categories = ThingCategoryUtility.GetThingCategoryDefs();
            foreach (var category in categories)
            {
                if (!Settings.CategoryLabelCache.TryGetValue(category.defName, out var categoryLabelInfo))
                    continue;

                if (category.label != categoryLabelInfo.CurrentCategoryLabel)
                {
                    category.label = categoryLabelInfo.CurrentCategoryLabel;
                    ToLog($"Set category label for [{category.defName}] to [{category.label}]");
                }
            }
        }
    }
}

