namespace NewHarvestPatches
{
    internal class CloneStockWithNewCategory : PatchOperationPathedExtended
    {
        private readonly string categoryType = null;
        private readonly bool removeOriginal = false; 
        private readonly bool zeroOutOriginal = true;
        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryType))
                    return false;

                string newCategoryDefNodeValue = GetCategoryName(xml, categoryType);
                if (string.IsNullOrEmpty(newCategoryDefNodeValue))
                    return false;

                if (!PreCheck(xpath, xml))
                    return false;

                bool modified = false;
                foreach (XmlNode liNode in nodes)
                {
                    XmlNode originalThingDefCountRangeNode = liNode.SelectSingleNode("thingDefCountRange");
                    if (originalThingDefCountRangeNode == null)
                        continue;

                    string thingDefCountRangeValue = originalThingDefCountRangeNode.InnerText;
                    if (string.IsNullOrWhiteSpace(thingDefCountRangeValue) || thingDefCountRangeValue == "0")
                        continue; // No reason to clone 

                    string classCategoryDefName = liNode.SelectSingleNode("categoryDef")?.InnerText;
                    if (string.IsNullOrWhiteSpace(classCategoryDefName))
                        continue;

                    bool match = TextMatchesForCategory(classCategoryDefName, categoryType);
                    bool excluded = match && IsExcludedCategory(classCategoryDefName);
                    if (!match || excluded)
                        continue;

                    XmlNode clonedLiClassNode = liNode.CloneNode(true);
                    XmlNode clonedCategoryDefNode = clonedLiClassNode.SelectSingleNode("categoryDef");
                    if (clonedCategoryDefNode == null)
                        continue;

                    XmlNode thingCategoryDef = xml.SelectSingleNode($"/Defs/ThingCategoryDef[defName='{clonedCategoryDefNode.InnerText}']");
                    if (thingCategoryDef == null)
                    {
                        continue;
                    }

                    XmlNode fullCategoryParentNode = thingCategoryDef.SelectSingleNode("parent");
                    if (fullCategoryParentNode == null)
                    {
                        continue;
                    }

                    if (!CategoryParentMatches(categoryType, fullCategoryParentNode.InnerText))
                    {
                        continue;
                    }

                    clonedCategoryDefNode.InnerText = newCategoryDefNodeValue;

                    var stockGeneratorParent = liNode.ParentNode;
                    string fullPath = Settings.Logging ? GetFullPathWithDefName(stockGeneratorParent) : "";
                    ToLog($"Cloned li node: [{clonedLiClassNode.OuterXml}] to [{fullPath}].");

                    XmlNode liClassParent = liNode.ParentNode;
                    if (removeOriginal)
                    {
                        liClassParent.InsertAfter(clonedLiClassNode, liNode);
                        liClassParent.RemoveChild(liNode);
                        modified = true;
                        ToLog($"Removed original li node: [{liNode.OuterXml}] from [{fullPath}].", 1);
                    }
                    else if (zeroOutOriginal)
                    {
                        liClassParent.InsertAfter(clonedLiClassNode, liNode);
                        originalThingDefCountRangeNode.InnerText = "0";
                        modified = true;
                        ToLog($"Set thingDefCountRange to 0 on original li node: [{liNode.OuterXml}] in [{fullPath}].", 1);
                    }
                }
                return modified;
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod(), optMsg: $"{xpath}");
                return false;
            }
        }
    }
}