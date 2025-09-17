
namespace NewHarvestPatches
{
    internal class TryReplaceCategoriesWithCategoryOfType : PatchOperationPathedExtended
    {
        private readonly string categoryType = null;
        //protected override bool ApplyWorker(XmlDocument xml)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(categoryType))
        //            return false;

        //        string newCategoryDefName = GetCategoryName(xml, categoryType);
        //        if (string.IsNullOrEmpty(newCategoryDefName))
        //            return false;

        //        if (!PreCheck(xpath, xml))
        //            return false;

        //        bool applied = false;
        //        foreach (XmlNode thingDef in nodes)
        //        {
        //            if (thingDef == null)
        //                continue;

        //            var ownerDoc = thingDef.OwnerDocument;
        //            if (ownerDoc == null)
        //                continue;

        //            string thingDefName = GetThingDefName(thingDef);
        //            if (!ResolveCategory(xml, thingDefName, newCategoryDefName, out string resolvedCategory))
        //            {
        //                continue;
        //            }

        //            // Remove existing <thingCategories> node if it exists
        //            var thingCategoriesNode = thingDef.SelectSingleNode("thingCategories");
        //            string replacedOrAdded = "Added";
        //            if (thingCategoriesNode != null)
        //            {
        //                replacedOrAdded = "Replaced";
        //                thingDef.RemoveChild(thingCategoriesNode);
        //            }

        //            // Create new <thingCategories> node
        //            thingCategoriesNode = thingDef.OwnerDocument.CreateElement("thingCategories");

        //            // Set Inherit="False" attribute
        //            XmlAttribute attr = thingDef.OwnerDocument.CreateAttribute("Inherit");
        //            attr.Value = "False";
        //            thingCategoriesNode.Attributes.Append(attr);

        //            // Create <li> node with the new category
        //            XmlElement liNode = thingDef.OwnerDocument.CreateElement("li");
        //            liNode.InnerText = resolvedCategory;
        //            thingCategoriesNode.AppendChild(liNode);

        //            // Append the new <thingCategories> node to the ThingDef
        //            thingDef.AppendChild(thingCategoriesNode);
        //            applied = true;
        //            ToLog($"{replacedOrAdded} thingCategories for ThingDef [{thingDefName}] with [{newCategoryDefName}].", 0);
        //        }
        //        return applied;
        //    }
        //    catch (Exception ex)
        //    {
        //        ExToLog(ex, MethodBase.GetCurrentMethod(), optMsg: $"{xpath}");
        //        return false;
        //    }
        //}

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryType))
                    return false;

                string newCategoryDefName = GetCategoryName(xml, categoryType);
                if (string.IsNullOrEmpty(newCategoryDefName))
                    return false;

                if (!PreCheck(xpath, xml))
                    return false;

                foreach (XmlNode thingDef in nodes)
                {
                    if (thingDef == null)
                        continue;

                    var ownerDoc = thingDef.OwnerDocument;
                    if (ownerDoc == null)
                        continue;

                    string thingDefName = GetThingDefName(thingDef);
                    if (!ResolveCategory(xml, thingDefName, newCategoryDefName, out string resolvedCategory))
                    {
                        continue;
                    }
                }
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
