namespace NewHarvestPatches
{
    internal class TestCategory : PatchOperationPathedExtended
    {
        private readonly string categoryType = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryType))
                    return false;

                if (!PreCheck(xpath, xml))
                    return false;

                if (nodes[0].Name != "ThingCategoryDef")
                    return false;

                bool flag = false;
                foreach (XmlNode thingCategoryDefNode in nodes)
                {
                    string thingCategoryDefName = thingCategoryDefNode.SelectSingleNode("defName")?.InnerText;
                    if (string.IsNullOrWhiteSpace(thingCategoryDefName))
                        continue;

                    bool match = TextMatchesForCategory(thingCategoryDefName, categoryType);
                    bool excluded = match && IsExcludedCategory(thingCategoryDefName);

                    if (match && !excluded)
                    {
                        flag = true;
                        ModAddedCategoryTypeCache.Add(categoryType);
                        if (!ModAddedCategoryDictionary.TryGetValue(categoryType, out var cacheSet))
                        {
                            cacheSet = [];
                            ModAddedCategoryDictionary[categoryType] = cacheSet;
                        }
                        if (cacheSet.Add(thingCategoryDefName))
                        {
                            ToLog($"Matched ThingCategoryDef [{thingCategoryDefName}] for type [{categoryType}]", 0);
                        }
                    }
                }

                if (flag)
                {
                    if (caseTrue != null)
                        return caseTrue.Apply(xml);
                }
                else if (caseFalse != null)
                    return caseFalse.Apply(xml);
                return true;

            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod(), optMsg: $"{xpath}");
                return false;
            }
        }
    }
}
