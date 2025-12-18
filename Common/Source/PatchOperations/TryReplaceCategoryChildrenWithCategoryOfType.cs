namespace NewHarvestPatches
{
    internal class TryReplaceCategoryChildrenWithCategoryOfType : PatchOperationExtended
    {
        private readonly string categoryType = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryType))
                    return false;

                string newCategoryDefName = PatchOperationPathedExtended.GetCategoryName(xml, categoryType);
                if (string.IsNullOrEmpty(newCategoryDefName))
                    return false;

                if (ModAddedCategoryDictionary.NullOrEmpty())
                {
                    ToLog("ModAddedCategoryCache dictionary is null or empty", 2);
                    return false;
                }

                if (!ModAddedCategoryDictionary.TryGetValue(categoryType, out var cachedSet))
                {
                    ToLog($"No cached categories found for categoryType [{categoryType}]", 2);
                    return false;
                }

                if (cachedSet.NullOrEmpty())
                {
                    ToLog($"Cached category set is null or empty for categoryType [{categoryType}]", 2);
                    return false;
                }

                XmlNodeList thingDefNodeList = xml.SelectNodes("/Defs/ThingDef");
                if (thingDefNodeList == null || thingDefNodeList.Count == 0)
                    return false;

                foreach (var cachedCategoryName in cachedSet)
                {
                    var categoryNode = xml.SelectSingleNode($"/Defs/ThingCategoryDef[defName='{cachedCategoryName}']");
                    string thingCategoryDefName = categoryNode?.SelectSingleNode("defName")?.InnerText;
                    if (string.IsNullOrWhiteSpace(thingCategoryDefName))
                        continue;

                    var matchingThingDefs = FindThingDefsWithCategoryOrParentCategory(thingDefNodeList, thingCategoryDefName);
                    if (matchingThingDefs.Count > 0)
                    {
                        foreach (XmlNode thingDef in matchingThingDefs)
                        {
                            if (thingDef == null)
                                continue;

                            string thingDefName = PatchOperationPathedExtended.GetThingDefName(thingDef);

                            var thingCategoriesNode = thingDef.SelectSingleNode("thingCategories");
                            List<string> oldCategories = [];
                            if (thingCategoriesNode != null)
                            {
                                foreach (XmlNode li in thingCategoriesNode.SelectNodes("li"))
                                {
                                    if (!string.IsNullOrWhiteSpace(li.InnerText))
                                        oldCategories.Add(li.InnerText);
                                }
                            }

                            if (!PatchOperationPathedExtended.ResolveCategory(xml, thingDefName, newCategoryDefName, out string resolvedCategory))
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        ToLog($"No ThingDefs found with category or parent category [{thingCategoryDefName}]", 2);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
                return false;
            }
        }

        private static bool HasCategoryInSelfOrAncestors(
            XmlNode thingDef,
            string thingCategoryDefName,
            Dictionary<string, XmlNode> categoryDefNameToNode)
        {
            // Get direct categories from this ThingDef
            var cats = new List<string>();
            var tcNode = thingDef.SelectSingleNode("thingCategories");
            if (tcNode != null)
            {
                foreach (XmlNode li in tcNode.ChildNodes)
                {
                    if (li.Name == "li" && !string.IsNullOrEmpty(li.InnerText))
                        cats.Add(li.InnerText.Trim());
                }
            }

            // Check each category, walking its parent chain
            foreach (var cat in cats)
            {
                if (CategoryMatchesOrAncestor(cat, thingCategoryDefName, categoryDefNameToNode))
                    return true;
            }

            return false;
        }

        // Returns true if this category or any of its ancestor categories matches the target.
        private static bool CategoryMatchesOrAncestor(
            string categoryDefName,
            string targetCategoryDefName,
            Dictionary<string, XmlNode> categoryDefNameToNode)
        {
            var visited = new HashSet<string>();
            string current = categoryDefName;

            int iterations = 0;

            while (!string.IsNullOrEmpty(current))
            {
                // Cycle protection
                if (++iterations > Safety.CategoryTraversalLimit)
                {
                    ToLog($"Category traversal exceeded limit ({Safety.CategoryTraversalLimit}) for starting category [{categoryDefName}]. Aborting traversal.", 2);
                    return false;
                }

                if (iterations > 1)
                    ToLog($"Category traversal iteration #{iterations}; starting [{categoryDefName}], current [{current}], target [{targetCategoryDefName}].", 1);

                // Cycle protection
                if (!visited.Add(current))
                    return false;

                if (string.Equals(current, targetCategoryDefName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!categoryDefNameToNode.TryGetValue(current, out XmlNode node))
                {
                    // Missing node — normal termination
                    if (iterations > 1)
                        ToLog($"Category [{current}] not found in lookup while traversing from [{categoryDefName}] to [{targetCategoryDefName}]. Aborting.", 2);
                    return false;
                }

                // Get <parent> tag
                var parentNode = node.SelectSingleNode("parent");
                current = parentNode?.InnerText?.Trim();
            }

            return false;
        }


        private static List<XmlNode> FindThingDefsWithCategoryOrParentCategory(XmlNodeList thingDefNodeList, string thingCategoryDefName)
        {
            // Build a lookup of defName -> ThingDef node for quick parent lookup
            var defNameToNode = new Dictionary<string, XmlNode>();

            // Map of parent Name attribute -> list of concrete child ThingDefs
            var parentToChildren = new Dictionary<string, List<XmlNode>>();

            // All concrete ThingDefs (those with a defName)
            var concreteThingDefs = new List<XmlNode>();

            foreach (XmlNode thingDef in thingDefNodeList)
            {
                var defName = thingDef.SelectSingleNode("defName")?.InnerText;
                if (!string.IsNullOrEmpty(defName))
                {
                    defNameToNode[defName] = thingDef;
                    concreteThingDefs.Add(thingDef);

                    // Record this concrete ThingDef as a child of its parent, if any
                    var parentNameAttr = thingDef.Attributes?["ParentName"]?.Value;
                    if (!string.IsNullOrEmpty(parentNameAttr))
                    {
                        if (!parentToChildren.TryGetValue(parentNameAttr, out var children))
                        {
                            children = [];
                            parentToChildren[parentNameAttr] = children;
                        }
                        children.Add(thingDef);
                    }
                }
            }

            // This will hold all concrete ThingDefs that should have the category
            var result = new List<XmlNode>();

            // Check each concrete ThingDef first (direct match)
            foreach (var thingDef in concreteThingDefs)
            {
                if (HasCategoryInSelfOrAncestors(thingDef, thingCategoryDefName, defNameToNode))
                {
                    result.Add(thingDef);
                }
            }

            // Now check each abstract parent with a Name attribute
            foreach (XmlNode parentDef in thingDefNodeList)
            {
                // Skip concrete ThingDefs - we already processed them above
                if (parentDef.SelectSingleNode("defName") != null)
                    continue;

                // Check if this is an abstract parent def with a Name attribute
                var nameAttr = parentDef.Attributes?["Name"];
                if (nameAttr != null && !string.IsNullOrEmpty(nameAttr.Value))
                {
                    string parentName = nameAttr.Value;

                    // Does this parent have the category?
                    if (HasCategoryInSelf(parentDef, thingCategoryDefName))
                    {
                        // If this parent has children, include all its concrete descendants
                        // that don't have Inherit="False"
                        if (parentToChildren.TryGetValue(parentName, out var directChildren))
                        {
                            foreach (var child in directChildren)
                            {
                                // Check if the child has Inherit="False" on its thingCategories node
                                var thingCategoriesNode = child.SelectSingleNode("thingCategories");
                                var inheritAttr = thingCategoriesNode?.Attributes?["Inherit"];
                                bool skipInheritance = inheritAttr != null &&
                                                      string.Equals(inheritAttr.Value, "False", StringComparison.OrdinalIgnoreCase);

                                // If the child doesn't override the inheritance, add it
                                if (!skipInheritance && !result.Contains(child))
                                {
                                    result.Add(child);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        // Helper to check if a node directly has the category (no parent checking)
        private static bool HasCategoryInSelf(XmlNode thingDef, string thingCategoryDefName)
        {
            var thingCategoriesNode = thingDef.SelectSingleNode("thingCategories");
            if (thingCategoriesNode != null)
            {
                var categories = thingCategoriesNode.SelectNodes("li");
                return categories != null && categories.Cast<XmlNode>().Any(li => li.InnerText == thingCategoryDefName);
            }
            return false;
        }
    }
}