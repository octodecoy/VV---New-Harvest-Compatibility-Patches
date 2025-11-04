namespace NewHarvestPatches
{
    internal static class DefHelpers
    {
        internal static List<ThingCategoryDef> GetThingCategoryDefs()
        {
            return
            [
                NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_AnimalFoods,
                NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Fruit,
                NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Grains,
                NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Nuts,
                NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Vegetables
            ];
        }

        public static Dictionary<ThingDef, (bool canBeFuel, bool isWood)> IndustrialResourceDefDictionary => GetIndustrialResourceDefDictionary();
        public static List<ThingDef> DeciduousTreeDefs => GetDeciduousTreeDefs();

        public static List<string> ExtractNamesFromEnabledSettings(string substring)
        {
            return [.. EnabledSettings
                .Where(s => s.StartsWith(substring))
                .Select(s => s.Substring(substring.Length))];
        }

        public static T GetRandomizedDefForIcon<T>(params string[] defNames) where T : Def
        {
            if (defNames.NullOrEmpty())
                return null;

            return DefDatabase<T>.GetNamedSilentFail(defNames[Rand.Range(0, defNames.Length)]);
        }

        public static List<T> GetDefsOfTypeByModContentPack<T>(params string[] packageIDs) where T : Def
        {
            // Not as helpful as initially thought, as patch added/replaced defs can have null modcontentpack
            if (packageIDs.NullOrEmpty())
                return [];

            var packageIDsList = packageIDs.ToList();
            packageIDsList.Add(NewHarvestPatchesMod.Instance.MetaData.PackageId); // Our own packageID

            return [.. DefDatabase<T>.AllDefsListForReading
                .Where(d => d.modContentPack?.PackageId is string id &&
                            packageIDsList.Any(pid => string.Equals(pid, id, StringComparison.OrdinalIgnoreCase)))];
        }


        public static List<T> GetDefsOfTypeByDefNames<T>(bool order = true, params string[] defNames) where T : Def
        {
            if (defNames.NullOrEmpty())
                return [];

            var defs = new List<T>();
            foreach (var defName in defNames)
            {
                var def = DefDatabase<T>.GetNamedSilentFail(defName);
                if (def != null)
                    defs.Add(def);
            }
            return order ? [.. defs.OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false)).ToList()] : defs;
        }

        private static Dictionary<ThingDef, (bool canBeFuel, bool isWood)> GetIndustrialResourceDefDictionary(bool order = true)
        {
            // canBeFuel = stuffProps.categories contains "Wood" ("Woody, RawWood, etc)
            // isWood = defName endswith "Wood" or "Lumber"

            var defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(td => td.HasCommonality(contains: ModName.Prefix.VV_) &&
                             td.stuffProps?.categories != null)
                .ToList();

            if (order)
            {
                defs = [.. defs.OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false)).ToList()];
            }

            const string wood = "Wood";
            const string lumber = "Lumber";
            var dictionary = new Dictionary<ThingDef, (bool, bool)>();
            foreach (var def in defs)
            {
                dictionary[def] = (
                    def.stuffProps.categories.Any(cat => cat.defName.ContainsIgnoreCase(wood)), 
                    def.defName.EndsWith(wood) || def.defName.EndsWith(lumber));
            };

            return dictionary;
        }

        private static List<ThingDef> GetTreeDefs(bool order = true, bool resourceOnly = false)
        {
            // resourceOnly for trees that produce wood (except silk?)
            const string tree = "Tree";
            var defs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(td => td.IsPlant(startsWith: ModName.Prefix.VV_, endsWith: tree) &&
                             (!resourceOnly || td.plant.IsTree))
                .ToList();

            return order ? [.. defs.OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false)).ToList()] : defs;
        }

        //private static List<ThingDef> GetNonTreePlantDefs(bool order = true)
        //{
        //    const string tree = "Tree";
        //    var defs = DefDatabase<ThingDef>.AllDefsListForReading
        //        .Where(td => td.IsPlant(startsWith: Prefix, endsWith: tree, reverseEnd: true))
        //        .ToList();

        //    return order ? [.. defs.OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false)).ToList()] : defs;
        //}

        public static (List<ThingDef> trees, List<ThingDef> plants) GetAllPlantDefs(bool order = true)
        {
            var allPlants = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(td => td.IsPlant(startsWith: ModName.Prefix.VV_, endsWith: ""))
                .ToList();

            if (allPlants.NullOrEmpty())
            {
                ToLog("No plants found.", 1);
                return ([], []);
            }

            const string tree = "Tree";
            var trees = allPlants
                .Where(td => td.defName.EndsWith(tree))
                .ToList();

            var plants = allPlants.Except(trees).ToList();

            if (order)
            {
                return (trees.OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false)).ToList(), plants.OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false)).ToList());
            }

            return (trees, plants);
        }

        private static List<ThingDef> GetDeciduousTreeDefs(bool order = true)
        {
            try
            {
                var trees = GetTreeDefs();
                if (trees.NullOrEmpty())
                {
                    ToLog("No deciduous trees found.", 2);
                    return [];
                }

                var nameField = typeof(ShaderParameter).GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField == null)
                {
                    ToLog("Could not access ShaderParameter.name field.", 2);
                    return [];
                }

                var valueField = typeof(ShaderParameter).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
                if (valueField == null)
                {
                    ToLog("Could not access ShaderParameter.value field.", 2);
                    return [];
                }

                const string targetName = "_FallBehaviorEnabled";
                var result = new List<ThingDef>();
                foreach (var tree in trees)
                {
                    var param = tree.graphicData?.shaderParameters?.FirstOrDefault(p =>
                    {
                        return (string)nameField.GetValue(p) == targetName;
                    });

                    if (param != null)
                    {
                        var value = (Vector4)valueField.GetValue(param);
                        if (value.x == 1f)
                        {
                            result.Add(tree);
                        }
                    }
                }
                return order ? [.. result.OrderBy(td => td.defName)] : result;
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
                return [];
            }
        }

        public static List<ThingDef> GetChildDefsOfCategory(ThingCategoryDef category, bool newHarvestOnly)
        {
            if (category == null)
                return [];

            return [.. category.childThingDefs
                .Where(def => def != null && 
                              (!newHarvestOnly || def.defName.StartsWith(ModName.Prefix.VV_)))];
        }

        public static List<ThingDef> GetAnimalFoodDefs(string startsWith = ModName.Prefix.VV_)
        {
            return [.. DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsAnimalFood(startsWith: startsWith))];
        }
    }
}