namespace NewHarvestPatches
{
    internal class AddCategoryToFilter : PatchOperationPathedExtended
    {
        private readonly string categoryType = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryType))
                    return false;

                string categoryDefName = GetCategoryName(xml, categoryType);
                if (string.IsNullOrEmpty(categoryDefName))
                    return false;

                if (!PreCheck(xpath, xml))
                    return false;

                if (nodes[0].Name != "categories")
                    return false;

                bool modified = false;
                foreach (XmlNode categoriesNode in nodes)
                {
                    bool anyMatch = false;
                    bool anyExcluded = false;
                    foreach (XmlNode liNode in categoriesNode.ChildNodes)
                    {
                        string defNameInLiNode = liNode.InnerText;  

                        XmlNode categoryDefNode = xml.SelectSingleNode($"/Defs/ThingCategoryDef[defName='{defNameInLiNode}']");
                        if (categoryDefNode == null)
                        {
                            continue;
                        }

                        XmlNode parentNode = categoryDefNode.SelectSingleNode("parent");
                        if (parentNode == null || !CategoryParentMatches(categoryType, parentNode.InnerText))
                        {
                            continue;
                        }

                        bool match = TextMatchesForCategory(defNameInLiNode, categoryType);
                        bool excluded = match && IsExcludedCategory(defNameInLiNode);
                        if (match)
                            anyMatch = true;
                        if (excluded)
                            anyExcluded = true;
                    }

                    bool canAdd = anyMatch && !anyExcluded;
                    if (canAdd)
                    {
                        XmlNode newLiNode = categoriesNode.OwnerDocument.CreateElement("li");
                        newLiNode.InnerText = categoryDefName;
                        categoriesNode.AppendChild(newLiNode);
                        ToLog($"Added category [{categoryDefName}] to [{(Settings.Logging ? GetFullPathWithDefName(categoriesNode) : "")}].");
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