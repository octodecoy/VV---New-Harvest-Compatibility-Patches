namespace NewHarvestPatches
{
    internal class HasModAddedCategoryOfType : PatchOperationExtended
    {
        private readonly string categoryType = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (string.IsNullOrWhiteSpace(categoryType))
                return false;

            if (ModAddedCategoryTypeCache == null)
                return false;

            // If contains, then a mod added category of that type was found and cached
            if (ModAddedCategoryTypeCache.Contains(categoryType))
            {
                if (caseTrue != null)
                    return caseTrue.Apply(xml);
            }
            else if (caseFalse != null)
                return caseFalse.Apply(xml);
            return true;
        }
    }
}
