namespace NewHarvestPatches
{
    internal class CheckboxInfo(
        string settingName,
        Func<bool> getter = null,
        Action<bool> setter = null,
        SettingsTab tab = SettingsTab.General,
        bool defaultValue = false,
        string labelNoTranslate = null,
        bool hasSubLabel = true,
        Def defForIcon = null,
        string sectionStartLabel = null,
        string sectionStartSubLabel = null,
        bool onlyOnOffSublabel = false,
        bool paintable = false,
        int indentSpaces = 0,
        float gapAfterCheckbox = 12f,
        (string key, string[] values)? labelOverride = null,
        (string key, string[] values)? subLabelOverride = null,
        ThingCategoryDef categoryDefForButton = null,
        CheckboxInfo parent = null,
        List<CheckboxInfo> children = null,
        bool drawSeparatorLine = false)
    {
        public string SettingName { get; private set; } = settingName;
        public Func<bool> Getter { get; private set; } = getter;
        public Action<bool> Setter { get; private set; } = setter;
        public SettingsTab Tab { get; private set; } = tab;
        public bool DefaultValue { get; private set; } = defaultValue;
        public string LabelNoTranslate { get; private set; } = labelNoTranslate;
        public bool HasSubLabel { get; private set; } = hasSubLabel;
        public Def DefForIcon { get; private set; } = defForIcon;
        public string SectionStartLabel { get; private set; } = sectionStartLabel;
        public string SectionStartSubLabel { get; private set; } = sectionStartSubLabel;
        public bool OnlyOnOffSublabel { get; private set; } = onlyOnOffSublabel;
        public bool Paintable { get; private set; } = paintable;
        public int IndentSpaces { get; private set; } = indentSpaces;
        public float GapAfterCheckbox { get; private set; } = gapAfterCheckbox;
        public (string key, string[] values)? LabelOverride { get; private set; } = labelOverride;
        public (string key, string[] values)? SubLabelOverride { get; private set; } = subLabelOverride;
        public ThingCategoryDef CategoryDefForButton { get; private set; } = categoryDefForButton;
        public CheckboxInfo Parent { get; private set; } = parent;
        public List<CheckboxInfo> Children { get; private set; } = children;
        public bool DrawSeparatorLine { get; private set; } = drawSeparatorLine;

        public static CheckboxInfo GetUltimateParent(CheckboxInfo child)
        {
            if (child == null) 
                return null;

            CheckboxInfo current = child;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }

        public static bool IsSelfOrAnyParentUnchecked(CheckboxInfo info)
        {
            if (info.Getter == null || !info.Getter())
                return true;
            var parent = info.Parent;
            while (parent != null)
            {
                if (parent.Getter == null || !parent.Getter())
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        internal static void BuildCheckboxInfo(ref List<CheckboxInfo> checkboxInfo)
        {
            var settings = Settings;
            var list = new List<CheckboxInfo>
            {
                new(
                    settingName: nameof(settings.Logging),
                    getter: () => settings.Logging,
                    setter: v => settings.Logging = v,
                    defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail($"{ModName.Prefix.VV_NHCP_}Logging_UIDef")
                )
            };

            if (HasForageModule)
            {
                string categoryKey = $"{TKey.Type.General}_{Category.Type.AnimalFoods}";
                var addToCategory = new CheckboxInfo(
                    settingName: nameof(settings.AddToAnimalFoodsCategory),
                    getter: () => settings.AddToAnimalFoodsCategory,
                    setter: v => settings.AddToAnimalFoodsCategory = v,
                    tab: SettingsTab.Categories,
                    defForIcon: ThingDefOf.Hay,
                    gapAfterCheckbox: 0f,
                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.AddToCategory}", [categoryKey]),
                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.AddToCategory}", [categoryKey, categoryKey]),
                    children:
                    [
                        new(
                            settingName: nameof(settings.MergeAnimalFoodsCategory),
                            getter: () => settings.MergeAnimalFoodsCategory,
                            setter: v => settings.MergeAnimalFoodsCategory = v,
                            indentSpaces: 1,
                            gapAfterCheckbox: 0f,
                            labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.MergeCategory}", [categoryKey]),
                            subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.MergeCategory}", [categoryKey, categoryKey]),
                            categoryDefForButton: NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_AnimalFoods,
                            children:
                            [
                                new(
                                    settingName: nameof(settings.AnimalFoodsCategoryResourceReadout),
                                    getter: () => settings.AnimalFoodsCategoryResourceReadout,
                                    setter: v => settings.AnimalFoodsCategoryResourceReadout = v,
                                    indentSpaces: 1,
                                    gapAfterCheckbox: 36f,
                                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    drawSeparatorLine: true
                                )
                            ]
                        )
                    ]
                );
                list.Add(addToCategory);

                list.Add(new CheckboxInfo(
                    settingName: nameof(settings.AddHayConversionRecipe),
                    getter: () => settings.AddHayConversionRecipe,
                    setter: v => settings.AddHayConversionRecipe = v,
                    tab: SettingsTab.Crafting,
                    defForIcon: ThingDefOf.Hay
                ));

                list.Add(new CheckboxInfo(
                    settingName: nameof(settings.HayNeedsCooling),
                    getter: () => settings.HayNeedsCooling,
                    setter: v => settings.HayNeedsCooling = v,
                    defForIcon: ThingDefOf.Hay,
                    defaultValue: true
                ));
            }
            else
            {
                settings.AddToAnimalFoodsCategory = false;
                settings.MergeAnimalFoodsCategory = false;
                settings.AnimalFoodsCategoryResourceReadout = false;
            }



            if (HasAnyFruit)
            {
                string categoryKey = $"{TKey.Type.General}_{Category.Type.Fruit}";
                var addToCategory = new CheckboxInfo(
                    settingName: nameof(settings.AddToFruitCategory),
                    getter: () => settings.AddToFruitCategory,
                    setter: v => settings.AddToFruitCategory = v,
                    tab: SettingsTab.Categories,
                    defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail("RawBerries"),
                    gapAfterCheckbox: 0f,
                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.AddToCategory}", [categoryKey]),
                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.AddToCategory}", [categoryKey, categoryKey]),
                    children:
                    [
                        new(
                            settingName: nameof(settings.MergeFruitCategory),
                            getter: () => settings.MergeFruitCategory,
                            setter: v => settings.MergeFruitCategory = v,
                            indentSpaces: 1,
                            gapAfterCheckbox: 0f,
                            labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.MergeCategory}", [categoryKey]),
                            subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.MergeCategory}", [categoryKey, categoryKey]),
                            categoryDefForButton: NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Fruit,
                            children:
                            [
                                new(
                                    settingName: nameof(settings.FruitCategoryResourceReadout),
                                    getter: () => settings.FruitCategoryResourceReadout,
                                    setter: v => settings.FruitCategoryResourceReadout = v,
                                    indentSpaces: 1,
                                    gapAfterCheckbox: 36f,
                                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    drawSeparatorLine: true
                                )
                            ]
                        )
                    ]
                );
                list.Add(addToCategory);
            }
            else
            {
                settings.AddToFruitCategory = false;
                settings.MergeFruitCategory = false;
                settings.FruitCategoryResourceReadout = false;
            }

            if (HasGardenModule)
            {
                string categoryKey = $"{TKey.Type.General}_{Category.Type.Grains}";
                var addToCategory = new CheckboxInfo(
                    settingName: nameof(settings.AddToGrainsCategory),
                    getter: () => settings.AddToGrainsCategory,
                    setter: v => settings.AddToGrainsCategory = v,
                    tab: SettingsTab.Categories,
                    defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail("RawCorn"),
                    gapAfterCheckbox: 0f,
                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.AddToCategory}", [categoryKey]),
                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.AddToCategory}", [categoryKey, categoryKey]),
                    children:
                    [
                        new(
                            settingName: nameof(settings.MergeGrainsCategory),
                            getter: () => settings.MergeGrainsCategory,
                            setter: v => settings.MergeGrainsCategory = v,
                            indentSpaces: 1,
                            gapAfterCheckbox: 0f,
                            labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.MergeCategory}", [categoryKey]),
                            subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.MergeCategory}", [categoryKey, categoryKey]),
                            categoryDefForButton: NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Grains,
                            children:
                            [
                                new(
                                    settingName: nameof(settings.GrainsCategoryResourceReadout),
                                    getter: () => settings.GrainsCategoryResourceReadout,
                                    setter: v => settings.GrainsCategoryResourceReadout = v,
                                    indentSpaces: 1,
                                    gapAfterCheckbox: 36f,
                                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    drawSeparatorLine: true
                                )
                            ]
                        )
                    ]
                );
                list.Add(addToCategory);
            }
            else
            {
                settings.AddToGrainsCategory = false;
                settings.MergeGrainsCategory = false;
                settings.GrainsCategoryResourceReadout = false;
            }

            if (HasAnyVegetables)
            {
                var categoryKey = $"{TKey.Type.General}_{Category.Type.Vegetables}";
                var addToCategory = new CheckboxInfo(
                    settingName: nameof(settings.AddToVegetablesCategory),
                    getter: () => settings.AddToVegetablesCategory,
                    setter: v => settings.AddToVegetablesCategory = v,
                    tab: SettingsTab.Categories,
                    defForIcon: ThingDefOf.RawPotatoes,
                    gapAfterCheckbox: 0f,
                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.AddToCategory}", [categoryKey]),
                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.AddToCategory}", [categoryKey, categoryKey]),
                    children:
                    [
                        new(
                            settingName: nameof(settings.MergeVegetablesCategory),
                            getter: () => settings.MergeVegetablesCategory,
                            setter: v => settings.MergeVegetablesCategory = v,
                            indentSpaces: 1,
                            gapAfterCheckbox: 0f,
                            labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.MergeCategory}", [categoryKey]),
                            subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.MergeCategory}", [categoryKey, categoryKey]),
                            categoryDefForButton: NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Vegetables,
                            children:
                            [
                                new(
                                    settingName: nameof(settings.VegetablesCategoryResourceReadout),
                                    getter: () => settings.VegetablesCategoryResourceReadout,
                                    setter: v => settings.VegetablesCategoryResourceReadout = v,
                                    indentSpaces: 1,
                                    gapAfterCheckbox: 36f,
                                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    drawSeparatorLine: true
                                )
                            ]
                        )
                    ]
                );
                list.Add(addToCategory);
            }
            else
            {
                settings.AddToVegetablesCategory = false;
                settings.MergeVegetablesCategory = false;
                settings.VegetablesCategoryResourceReadout = false;
            }

            if (HasTreesModule)
            {
                string categoryKey = $"{TKey.Type.General}_{Category.Type.Nuts}";
                var addToCategory = new CheckboxInfo(
                    settingName: nameof(settings.AddToNutsCategory),
                    getter: () => settings.AddToNutsCategory,
                    setter: v => settings.AddToNutsCategory = v,
                    tab: SettingsTab.Categories,
                    defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail("VV_Chestnuts"),
                    gapAfterCheckbox: 0f,
                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.AddToCategory}", [categoryKey]),
                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.AddToCategory}", [categoryKey, categoryKey]),
                    children:
                    [
                        new(
                            settingName: nameof(settings.MergeNutsCategory),
                            getter: () => settings.MergeNutsCategory,
                            setter: v => settings.MergeNutsCategory = v,
                            indentSpaces: 1,
                            gapAfterCheckbox: 0f,
                            labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.MergeCategory}", [categoryKey]),
                            subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.MergeCategory}", [categoryKey, categoryKey]),
                            categoryDefForButton: NHCP_ThingCategoryDefOf.VV_NHCP_DummyCategory_Nuts,
                            children:
                            [
                                new(
                                    settingName: nameof(settings.NutsCategoryResourceReadout),
                                    getter: () => settings.NutsCategoryResourceReadout,
                                    setter: v => settings.NutsCategoryResourceReadout = v,
                                    indentSpaces: 1,
                                    gapAfterCheckbox: 36f,
                                    labelOverride: ($"{TKey.Type.CheckboxLabel}_{Category.Suffix.ResourceReadout}", [categoryKey]),
                                    subLabelOverride: ($"{TKey.Type.CheckboxSubLabel}_{Category.Suffix.ResourceReadout}", [categoryKey])
                                )
                            ]
                        )
                    ]
                );
                list.Add(addToCategory);
            }
            else
            {
                settings.AddToNutsCategory = false;
                settings.MergeNutsCategory = false;
                settings.NutsCategoryResourceReadout = false;
            }

            List<string> defForIconList = [];
            if (HasMedicinalModule && HasVanillaBrewingExpanded)
            {
                defForIconList = [.. DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(d => d?.defName?.StartsAndEndsWith(start: ModName.Prefix.VV_, end: "Tea") == true && d.IsIngestible)
                    .Select(d => d.defName)];
                var defForIcon = defForIconList.Count > 0 ? GetRandomizedDefForIcon<ThingDef>([.. defForIconList]) : null;
                list.Add(new CheckboxInfo(
                    settingName: nameof(settings.MoveDrinksToVBECategory),
                    getter: () => settings.MoveDrinksToVBECategory,
                    setter: v => settings.MoveDrinksToVBECategory = v,
                    defForIcon: defForIcon
                ));
            }

            if (HasIndustrialModule)
            {
                list.Add(new CheckboxInfo(
                    settingName: nameof(settings.UseVanillaLogGraphic),
                    getter: () => settings.UseVanillaLogGraphic,
                    setter: v => settings.UseVanillaLogGraphic = v,
                    tab: SettingsTab.Visuals,
                    defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail($"{ModName.Prefix.VV_NHCP_}Log_UIDef"),
                    sectionStartLabel: "GeneralSection"
                ));

                List<TerrainDef> floorDefsForIcons = [];
                defForIconList = [.. DefDatabase<TerrainDef>.AllDefsListForReading
                    .Where(d => d.IsFloor &&
                                !d.bridge &&
                                d?.defName?.StartsWith(ModName.Prefix.VV_) == true &&
                                d.costList?.Count == 1 &&
                                d.costList[0]?.thingDef?.defName?.Contains("Wood") == true)
                    .Select(d => d.defName)];
                string[] floorDefNames = [.. defForIconList];
                for (int i = 0; i < floorDefNames.Length; i++)
                {
                    floorDefsForIcons.Add(DefDatabase<TerrainDef>.GetNamedSilentFail(floorDefNames[i]));
                }
                floorDefsForIcons.Shuffle();

                list.Add(new CheckboxInfo(
                    settingName: nameof(settings.AddMoreWoodFloors),
                    getter: () => settings.AddMoreWoodFloors,
                    setter: v => settings.AddMoreWoodFloors = v,
                    tab: SettingsTab.Floors,
                    defForIcon: DefDatabase<TerrainDef>.GetNamedSilentFail("WoodPlankFloor")
                ));

                if (!HasFernyFloorMenu)
                {
                    list.Add(new CheckboxInfo(
                        settingName: nameof(settings.NewHarvestWoodFloorsToDropdowns),
                        getter: () => settings.NewHarvestWoodFloorsToDropdowns,
                        setter: v => settings.NewHarvestWoodFloorsToDropdowns = v,
                        tab: SettingsTab.Floors,
                        defForIcon: DefDatabase<TerrainDef>.GetNamedSilentFail("VV_CedarFloor")
                    ));

                    list.Add(new CheckboxInfo(
                        settingName: nameof(settings.BaseWoodFloorsToDropdowns),
                        getter: () => settings.BaseWoodFloorsToDropdowns,
                        setter: v => settings.BaseWoodFloorsToDropdowns = v,
                        tab: SettingsTab.Floors,
                        defForIcon: DefDatabase<TerrainDef>.GetNamedSilentFail("VV_MahoganyFloor")
                    ));

                    list.Add(new CheckboxInfo(
                        settingName: nameof(settings.ModWoodFloorsToDropdowns),
                        getter: () => settings.ModWoodFloorsToDropdowns,
                        setter: v => settings.ModWoodFloorsToDropdowns = v,
                        tab: SettingsTab.Floors,
                        defForIcon: DefDatabase<TerrainDef>.GetNamedSilentFail("VV_RosewoodFloor")
                    ));
                }

                if (ShowWoodConvertRecipe)
                {
                    defForIconList = [.. IndustrialResourceDefDictionary?
                    .Where(kvp => kvp.Value.isWood)
                    .Select(kvp => kvp.Key.defName)];
                    var defForIcon = defForIconList.Count > 0 ? GetRandomizedDefForIcon<ThingDef>([.. defForIconList]) : null;
                    list.Add(new CheckboxInfo(
                        settingName: nameof(settings.AddWoodConversionRecipe),
                        getter: () => settings.AddWoodConversionRecipe,
                        setter: v => settings.AddWoodConversionRecipe = v,
                        tab: SettingsTab.Crafting,
                        defForIcon: defForIcon
                    ));
                }

                if (HasIdeology)
                {
                    defForIconList = [.. DefDatabase<GauranlenTreeModeDef>.AllDefsListForReading
                    .Where(d => d?.defName?.StartsWith(ModName.Prefix.VV_) == true &&
                                d.hyperlinks[0].def?.defName?.StartsWith(ModName.Prefix.VV_NHCP_) == true)
                    .Select(d => d.hyperlinks[0].def.defName)];
                    var defForIcon = defForIconList.Count > 0 ? GetRandomizedDefForIcon<ThingDef>([.. defForIconList]) : null;
                    list.Add(new CheckboxInfo(
                        settingName: nameof(settings.AddWoodDryads),
                        getter: () => settings.AddWoodDryads,
                        setter: v => settings.AddWoodDryads = v,
                        defForIcon: HasIndustrialModule ? defForIcon : null
                    ));
                }

                if (HasOdyssey)
                {
                    list.Add(new CheckboxInfo(
                        settingName: nameof(settings.AddAquaticReedsToBiomes),
                        getter: () => settings.AddAquaticReedsToBiomes,
                        setter: v => settings.AddAquaticReedsToBiomes = v,
                        defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail("VV_ReedPlant")
                    ));
                }
            }

            if (HasForageModule || HasIndustrialModule)
            {
                defForIconList = [.. DefDatabase<TerrainDef>.AllDefsListForReading
                    .Where(d => d?.defName?.StartsWith(ModName.Prefix.VV_) == true &&
                                !d.bridge &&
                                d.costList?.Count == 1 &&
                                d.costList[0]?.thingDef?.defName?.Contains("Wood") is false)
                    .Select(d => d.defName)];
                var defForIcon = defForIconList.Count > 0 ? GetRandomizedDefForIcon<TerrainDef>([.. defForIconList]) : null;
                list.Add(new CheckboxInfo(
                    settingName: nameof(settings.NewHarvestNonWoodFloorsToDropdown),
                    getter: () => settings.NewHarvestNonWoodFloorsToDropdown,
                    setter: v => settings.NewHarvestNonWoodFloorsToDropdown = v,
                    tab: SettingsTab.Floors,
                    defForIcon: defForIcon
                ));
            }

            if (ShowFuelSettings)
            {
                var dictionary = IndustrialResourceDefDictionary;
                if (dictionary.NullOrEmpty())
                {
                    ToLog($"Could not get Industrial defs for fuel dictionary.", 2);
                    settings.FuelTypes = [];
                }
                else
                {
                    settings.FuelTypes = settings.FuelTypes != null
                            ? settings.FuelTypes.RemoveNulls<ThingDef, string, bool>(includeValues: false, includeKeys: true)
                            : [];

                    foreach (var kvp in dictionary)
                    {
                        bool canBeFuel = kvp.Value.canBeFuel;
                        if (!canBeFuel)
                            continue; // Skip defs that cannot be fuel

                        string fuelKey = kvp.Key.defName;
                        if (kvp.Key.defName == null)
                            continue;

                        if (!settings.FuelTypes.ContainsKey(fuelKey))
                            settings.FuelTypes[fuelKey] = kvp.Value.isWood;

                        list.Add(new CheckboxInfo(
                            settingName: $"Fuel={fuelKey}",
                            getter: () => settings.FuelTypes.TryGetValue(fuelKey, out var val) && val,
                            setter: v => settings.FuelTypes[fuelKey] = v,
                            tab: SettingsTab.Fuel,
                            labelNoTranslate: kvp.Key.LabelCap.ToString() ?? kvp.Key.defName ?? "??",
                            defaultValue: kvp.Value.isWood,
                            defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail(fuelKey),
                            onlyOnOffSublabel: true
                        ));
                    }
                }
            }

            if (HasAnyTrees)
            {
                var treeList = DeciduousTreeDefs;
                if (treeList.NullOrEmpty())
                {
                    ToLog($"Could not get fall color tree defs.", 2);
                    settings.FallColorTrees = [];
                }
                else
                {
                    settings.FallColorTrees = settings.FallColorTrees != null
                            ? settings.FallColorTrees.RemoveNulls<ThingDef, string, bool>(includeValues: false, includeKeys: true)
                            : [];

                    bool addSectionLabel = true; // Only add a sectionStartLabel to the first one
                    foreach (var tree in treeList)
                    {
                        string treeKey = tree.defName;
                        if (treeKey == null)
                            continue;

                        if (!settings.FallColorTrees.ContainsKey(treeKey))
                            settings.FallColorTrees[treeKey] = true;

                        list.Add(new CheckboxInfo(
                            settingName: $"FallColorTree={treeKey}",
                            getter: () => settings.FallColorTrees.TryGetValue(treeKey, out var val) && val,
                            setter: v => settings.FallColorTrees[treeKey] = v,
                            labelNoTranslate: tree.LabelCap.ToString() ?? treeKey ?? "??",
                            tab: SettingsTab.Visuals,
                            defaultValue: true,
                            defForIcon: DefDatabase<ThingDef>.GetNamedSilentFail(treeKey),
                            hasSubLabel: false,
                            sectionStartLabel: addSectionLabel ? "FallColorTreesSection" : "",
                            sectionStartSubLabel: addSectionLabel ? "FallColorTreesSectionSub" : null,
                            paintable: true,
                            gapAfterCheckbox: 0f
                        ));
                        addSectionLabel = false;
                    }
                }
            }
            checkboxInfo = !list.NullOrEmpty() ? list : [];
        }
    }
}