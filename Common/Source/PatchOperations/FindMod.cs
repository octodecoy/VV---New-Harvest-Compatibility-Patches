namespace NewHarvestPatches
{
    internal class FindMod : PatchOperationExtended
    {
        private readonly List<string> mods = null;
        private readonly List<string> modGroupOne = null;
        private readonly List<string> modGroupTwo = null;
        private readonly bool packageID = false;
        private readonly Logic logicGroupOne = Logic.Or;
        private readonly Logic logicGroupTwo = Logic.Or;
        private readonly Logic logicBetweenGroups = Logic.Or;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool hasModsList = !mods.NullOrEmpty();
            bool hasModGroupOne = !modGroupOne.NullOrEmpty();
            bool hasModGroupTwo = !modGroupTwo.NullOrEmpty();
            if (!hasModsList && !hasModGroupOne && !hasModGroupTwo)
                return false;

            bool flag;
            if (hasModsList)
            {
                flag = GetFlag(mods, logic);
            }
            else if (hasModGroupOne && hasModGroupTwo)
            {
                flag = GetGroupFlag();
            }
            else
            {
                ToLog("No mods supplied for FindMod patch operation.", 1);
                return false;
            }

            if (flag)
            {
                if (caseTrue != null)
                {
                    return caseTrue.Apply(xml);
                }
            }
            else if (caseFalse != null)
            {
                return caseFalse.Apply(xml);
            }

            return true;
        }

        private bool GetGroupFlag()
        {
            bool groupOneFlag = GetFlag(modGroupOne, logicGroupOne);
            return logicBetweenGroups switch
            {
                Logic.Or => groupOneFlag || GetFlag(modGroupTwo, logicGroupTwo),
                Logic.And => groupOneFlag && (_ = GetFlag(modGroupTwo, logicGroupTwo)),
                // Just use <mods> and caseFalse for Not
                Logic.Xor => groupOneFlag ^ (_ = GetFlag(modGroupTwo, logicGroupTwo)),
                _ => false,
            };
        }

        private bool GetFlag(List<string> modsList, Logic thisLogic)
        {
            bool isInstalled(string mod) =>
                LoadedModManager.RunningMods.Any(m => (packageID ? m.PackageId.ToLower() : m.Name.ToLower()) == mod.ToLower());

            return EvaluateLogic(thisLogic, modsList, isInstalled);
        }
    }
}