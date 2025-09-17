namespace NewHarvestPatches
{
    public interface ISettingsMenuWindows { }
    public static class SettingWindows
    {
        public class ResizableWindow : Window, ISettingsMenuWindows
        {
            private readonly string _windowType;
            public ResizableWindow(string windowType, Vector2? size = null, string newOptionalTitle = "")
            {
                _windowType = windowType;
                optionalTitle = newOptionalTitle;
                if (size.HasValue)
                    _customSize = size.Value;
                forcePause = true;
                doCloseButton = true;
                doCloseX = true;
                absorbInputAroundWindow = true;
                resizeable = true;
            }

            private Vector2 _customSize = new(400f, 400f);
            public override Vector2 InitialSize => _customSize;

            private void DoResetWindow(Listing_Standard ls)
            {
                string buttonLabel = Translator.TranslateKey(TKey.Type.Tab, nameof(SettingsTab.AllTabs));
                if (ls.ButtonText(buttonLabel))
                {
                    Settings.DoResetSettingDialogue(SettingsTab.AllTabs);
                }

                for (int i = 0; i < _tabs.Count; i++)
                {
                    TabRecord tab = _tabs[i];
                    string currentTabLabel = "";
                    if ($"{tab.label}" == $"{_currentTab}")
                    {
                        currentTabLabel = " (" + Translator.TranslateKey(TKey.Type.Tab, "Current") + ")";
                    }
                    string tabLabel = tab.label;
                    buttonLabel = Translator.TranslateKey(TKey.Type.Tab, tabLabel) + currentTabLabel;
                    if (ls.ButtonText(buttonLabel))
                    {
                        if (Enum.TryParse<SettingsTab>(tabLabel, out var settingsTab))
                        {
                            Settings.DoResetSettingDialogue(settingsTab);
                        }
                    }
                }
            }

            private void DoModulesWindow(Listing_Standard ls)
            {
                var installedModules = NewHarvestVersions;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = cyan;
                foreach (var (version, key) in installedModules.Values)
                {
                    string text = $"{Translator.TranslateKey(TKey.Type.General, key)}: v{version}";
                    ls.Label(text);
                }
                GUI.color = white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            public override void DoWindowContents(Rect inRect)
            {
                var originalUIState = new UIState(GUI.color, Text.Anchor, Text.Font);

                var ls = new Listing_Standard();
                ls.Begin(inRect);

                if (_windowType == "Reset")
                {
                    DoResetWindow(ls);
                }
                else if (_windowType == "Modules")
                {
                    DoModulesWindow(ls);
                }

                ls.End();

                originalUIState.Restore();
            }
        }

        public class ThingDefSelectorWindow : Window, ISettingsMenuWindows
        {
            private readonly string _windowCategoryDefName;
            private readonly HashSet<string> _checkedDefNames = [];
            private List<ThingDef> _filteredThingDefs = null;
            private List<ThingDef> _allThingDefs = null;
            private Vector2 _scrollPosition = Vector2.zero;
            private readonly QuickSearchWidget _searchWidget = new();

            public override Vector2 InitialSize => new(650f, 600f);
            public override QuickSearchWidget CommonSearchWidget => _searchWidget;

            public ThingDefSelectorWindow(string categoryDefName, string newOptionalTitle)
            {
                _windowCategoryDefName = categoryDefName;
                forcePause = true;
                doCloseButton = true;
                doCloseX = true;
                absorbInputAroundWindow = true;
                resizeable = true;
                optionalTitle = newOptionalTitle;
            }

            public override void PreOpen()
            {
                base.PreOpen();

                // Get the currently checked defs for this category
                foreach (var defToCategoryInfo in Settings.CategoryData)
                {
                    if (defToCategoryInfo.CurrentCategoryName == _windowCategoryDefName && !defToCategoryInfo.IsCurrentCategoryUserDisabled)
                        _checkedDefNames.Add(defToCategoryInfo.ThingDefName);
                }

                GetFilteredData();
            }

            public override void PostClose()
            {
                Settings.WriteSettingsToFile();
            }

            public override void Notify_CommonSearchChanged()
            {
                FilterThingDefs();
            }

            private void GetFilteredData()
            {
                // Show all ThingDefs in CategoryData
                var allDefNames = Settings.CategoryData.Select(c => c.ThingDefName).ToList();

                _allThingDefs = [.. allDefNames
                    .Select(DefDatabase<ThingDef>.GetNamedSilentFail)
                    .Where(td => td != null)
                    .OrderBy(td => td.label, StringComparer.Create(CultureInfo.CurrentCulture, false))];

                FilterThingDefs();
            }

            private void FilterThingDefs()
            {
                if (_allThingDefs == null)
                {
                    _filteredThingDefs = null;
                    return;
                }

                string search = _searchWidget.filter.Text;
                if (string.IsNullOrWhiteSpace(search))
                {
                    _filteredThingDefs = _allThingDefs;
                    return;
                }

                string searchLower = search.ToLower();
                _filteredThingDefs = [.. _allThingDefs
                    .Where(def =>
                        (def.label?.ToLower().Contains(searchLower) == true) ||
                        (def.defName?.ToLower().Contains(searchLower) == true) ||
                        (
                            DefToCategoryInfo.TryGetDefToCategoryInfo(def.defName, out var defToCategoryInfo) &&
                            (
                                defToCategoryInfo.CurrentCategoryName.Contains(searchLower) ||
                                DefDatabase<ThingCategoryDef>.GetNamedSilentFail(defToCategoryInfo.CurrentCategoryName)?.label?.ToLower().Contains(searchLower) == true
                            )
                        )
                    )
                ];
            }

            public override void DoWindowContents(Rect inRect)
            {
                if (_allThingDefs == null || _filteredThingDefs == null)
                    return;

                const float rowHeight = 50f;
                const float gapBetweenRows = GenUI.GapTiny;
                const float rightMargin = GenUI.ScrollBarWidth * 2;
                const float closeButtonRowHeight = 55f;
                const float searchBarGap = GenUI.Pad;

                float reservedHeight = closeButtonRowHeight + QuickSearchSize.y + searchBarGap;
                Rect scrollableRect = new(inRect.x, inRect.y, inRect.width, inRect.height - reservedHeight);

                float contentHeight = (_filteredThingDefs.Count) * (rowHeight + gapBetweenRows);
                Rect viewRect = new(0, 0, scrollableRect.width - rightMargin, contentHeight);
                Widgets.BeginScrollView(scrollableRect, ref _scrollPosition, viewRect);

                float curY = 0f;
                foreach (var def in _filteredThingDefs)
                {
                    //var defToCategoryInfo = Settings.CategoryData.FirstOrDefault(c => c.ThingDefName == def.defName);
                    //if (defToCategoryInfo == null)
                    //{
                    //    // Redundant
                    //    ToLog($"ThingDef [{def.defName}] missing from CategoryData, skipping...", 2);
                    //    continue;
                    //}
                    if (!DefToCategoryInfo.TryGetDefToCategoryInfo(def.defName, out var defToCategoryInfo))
                    {
                        // Redundant
                        ToLog($"ThingDef [{def.defName}] missing from CategoryData, skipping...", 2);
                        continue;
                    }
                    string assignedCategoryDefName = defToCategoryInfo.CurrentCategoryName;

                    bool isAvailable = defToCategoryInfo.IsCurrentCategoryUserDisabled || assignedCategoryDefName == _windowCategoryDefName || assignedCategoryDefName == Category.Type.None_Base;

                    bool value = _checkedDefNames.Contains(def.defName) && !defToCategoryInfo.IsCurrentCategoryUserDisabled;

                    string label = def.LabelCap.ToString() ?? def.defName ?? "??";

                    Rect rowRect = new(0, curY, viewRect.width, rowHeight);

                    if (!isAvailable && Mouse.IsOver(rowRect))
                    {
                        Widgets.DrawTextHighlight(rowRect, expandBy: 0f, color: value ? RedHighlightColor : GreenHighlightColor);
                    }

                    Rect iconRect = rowRect.LeftPartPixels(rowRect.height);
                    Widgets.DefIcon(iconRect, def);

                    Rect checkboxRect = rowRect.RightPartPixels(rowRect.width - rowRect.height - GenUI.GapTiny);
                    Widgets.CheckboxLabeled(checkboxRect, label, ref value, disabled: !isAvailable);

                    TooltipHandler.TipRegion(checkboxRect, GetTooltip(def));

                    // Overlay the category label if disabled
                    if (!isAvailable)
                    {
                        var assignedCatDef = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(assignedCategoryDefName);
                        //string catLabel = assignedCatDef?.label ?? assignedCategoryDefName;
                        string catLabel = assignedCatDef?.LabelCap.ToString() ?? assignedCategoryDefName ?? "??";
                        Vector2 labelSize = GenUI.GetSizeCached(catLabel);

                        Rect overlayRect = new(checkboxRect.x + (checkboxRect.width - labelSize.x) / 2f,
                                              checkboxRect.y + (checkboxRect.height - labelSize.y) / 2f,
                                              labelSize.x, labelSize.y);

                        Color oldColor = GUI.color;
                        GUI.color = DisabledRowColor;
                        GUI.DrawTexture(checkboxRect, BaseContent.WhiteTex);
                        GUI.color = yellow;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(overlayRect, catLabel);
                        Text.Anchor = TextAnchor.UpperLeft;
                        GUI.color = oldColor;
                    }

                    // Add or remove from checked set
                    if (isAvailable)
                    {
                        bool wasChecked = _checkedDefNames.Contains(def.defName);
                        if (value && !wasChecked)
                        {
                            _checkedDefNames.Add(def.defName);
                            if (defToCategoryInfo == null)
                            {
                                // Create a new entry if it doesn't exist - not sure how this would happen
                                DefToCategoryInfo.CacheCategoryData(def.defName, Category.Type.None_Base, _windowCategoryDefName);
                            }
                            else
                            {
                                // Just enable the category without changing it
                                defToCategoryInfo.CurrentCategoryName = _windowCategoryDefName;
                                defToCategoryInfo.IsCurrentCategoryUserDisabled = false;
                            }
                        }
                        else if (!value && wasChecked)
                        {
                            _checkedDefNames.Remove(def.defName);
                            if (defToCategoryInfo != null)
                            {
                                if (defToCategoryInfo.OriginalCategoryName != Category.Type.None_Base)
                                {
                                    // Just disable it
                                    defToCategoryInfo.IsCurrentCategoryUserDisabled = true;
                                }
                                else
                                {
                                    // No original category, so set to None
                                    defToCategoryInfo.CurrentCategoryName = Category.Type.None_Base;
                                }
                            }
                        }
                    }

                    curY += rowHeight + gapBetweenRows;
                }

                Widgets.EndScrollView();

                // Reset button
                float buttonY = inRect.height - CloseButSize.y * 2;
                Rect resetButtonRect = new(inRect.width / 2f - CloseButSize.x / 2f, buttonY, CloseButSize.x, CloseButSize.y);

                if (Widgets.ButtonText(resetButtonRect, Translator.TranslateKey(TKey.Type.Button, "Reset")))
                {
                    DoResetCategoryDialogue(_windowCategoryDefName);
                }
            }

            private static string GetTooltip(ThingDef def)
            {
                return
                    TryGetModNameForThingDef(def) + "\n\n" +
                    (def?.defName ?? "") + "\n\n" +
                    (def?.description ?? "");
            }

            private static string TryGetModNameForThingDef(ThingDef def)
            {
                if (def?.modContentPack != null && !string.IsNullOrEmpty(def.modContentPack.Name))
                    return def.modContentPack.Name;
                return "??";
            }

            private void DoResetCategoryDialogue(string selectedCategoryDefName)
            {
                if (string.IsNullOrWhiteSpace(selectedCategoryDefName))
                    return;

                string label = Translator.TranslateKey(TKey.Type.Button, "CategoryResetInfo") + "\n\n" + Translator.TranslateKey(TKey.Type.Button, "AreYouSure"); 

                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    label,
                    delegate
                    {
                        // Reset all defs in this category to their original assignment, and all things in this category that originate elsewhere back to their origins
                        foreach (var defToCategoryInfo in Settings.CategoryData)
                        {
                            if (defToCategoryInfo.CurrentCategoryName == selectedCategoryDefName || defToCategoryInfo.OriginalCategoryName == selectedCategoryDefName)
                            {
                                defToCategoryInfo.IsCurrentCategoryUserDisabled = false;
                                defToCategoryInfo.CurrentCategoryName = defToCategoryInfo.OriginalCategoryName;
                            }
                        }

                        _checkedDefNames.Clear();
                        foreach (var info in Settings.CategoryData)
                        {
                            if (info.CurrentCategoryName == selectedCategoryDefName)
                                _checkedDefNames.Add(info.ThingDefName);
                        }

                        GetFilteredData();
                        Settings.WriteSettingsToFile();
                    }
                ));
            }
        }
    }
}
