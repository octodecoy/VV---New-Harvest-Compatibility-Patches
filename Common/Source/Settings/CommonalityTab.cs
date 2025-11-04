namespace NewHarvestPatches
{
    internal static class CommonalityTab
    {
        private const float CommonalityMin = 0f;
        private const float CommonalityMax = 100f;
        private const float FloatTolerance4Decimals = 0.0001f;
        public static readonly Dictionary<string, string> _commonalityBuffers = [];
        private static readonly bool _showTextField = !HasBetterSliders;
        private static readonly bool _showAllSliders = ShowVEFCommonalitySettings;
        private static readonly float _sliderGroupHeight = _showAllSliders ? 160f : 100f; // More height if VEF is installed for 2 more sliders

        public static float DoCommonalitySliders(Listing_Standard ls, Dictionary<string, CommonalityInfo> stuffCommonality)
        {
            const float pad = GenUI.Pad;
            bool settingChanged = false;

            // Draw description
            DrawCustomLabel(ls, Translator.TranslateKey(TKey.Type.TabSubLabel, "CommonalityDescription"), font: GameFont.Tiny, anchor: TextAnchor.MiddleCenter);
            ls.Gap();

            // Prepare labels and calculate max width once
            (string[] labels, float maxLabelWidth) = PrepareLabels();
            var originalUIState = new UIState(GUI.color, Text.Anchor, Text.Font);

            // Process each stuff item
            foreach (var kvp in stuffCommonality)
            {
                // Draw group container
                Rect groupBorderRect = ls.GetRect(_sliderGroupHeight);
                DrawMenuSection(groupBorderRect, thickness: 1, bgColor: BackgroundGrayWithAlpha);
                Rect groupRect = groupBorderRect.GetInnerRect();

                var groupListing = new Listing_Standard();
                groupListing.Begin(groupRect);

                // Draw header
                string headerLabel = $"[{kvp.Value.DefLabel} – {Translator.TranslateComposite($"{SettingsTab.General}_Default", [($"{kvp.Value.DefaultCommonality}]", false)])}";
                DrawCustomLabel(groupListing, headerLabel, subLabel: false, category: false, anchor: TextAnchor.MiddleCenter, color: Color.cyan);
                groupListing.Gap(pad);

                // Draw sliders based on mode
                if (_showAllSliders)
                {
                    // VEF mode - show separate sliders for different categories
                    settingChanged |= DrawCategorySlider(groupListing, kvp.Key, ref kvp.Value.StructureOffset, "_Structure", labels[0], maxLabelWidth);
                    groupListing.Gap(pad);

                    settingChanged |= DrawCategorySlider(groupListing, kvp.Key, ref kvp.Value.WeaponOffset, "_Weapon", labels[1], maxLabelWidth);
                    groupListing.Gap(pad);

                    settingChanged |= DrawCategorySlider(groupListing, kvp.Key, ref kvp.Value.ApparelOffset, "_Apparel", labels[2], maxLabelWidth);
                }
                else
                {
                    // Standard mode - single slider for core commonality
                    settingChanged |= DrawCategorySlider(groupListing, kvp.Key, ref kvp.Value.CoreCommonality, "_Core", labels[0], maxLabelWidth);
                }

                groupListing.End();
                ls.Gap(GenUI.GapWide);
            }

            SettingChanged |= settingChanged;
            originalUIState.Restore();
            return ls.CurHeight;

            // Local function to prepare labels and calculate max width
            static (string[] labels, float maxWidth) PrepareLabels()
            {
                if (_showAllSliders)
                {
                    string[] categoryLabels = [
                        Translator.TranslateKey(TKey.Type.SliderLabel, "StructureCommonality") + " = ",
                        Translator.TranslateKey(TKey.Type.SliderLabel, "WeaponCommonality") + " = ",
                        Translator.TranslateKey(TKey.Type.SliderLabel, "ApparelCommonality") + " = "
                    ];
                    return (categoryLabels, categoryLabels.Max(l => l.GetWidthCached()));
                }
                else
                {
                    string commonalityLabel = Translator.TranslateKey(TKey.Type.SliderLabel, "Commonality") + " = ";
                    return (new[] { commonalityLabel }, commonalityLabel.GetWidthCached());
                }
            }
        }

        private static bool DrawCategorySlider(Listing_Standard listing, string key, ref float value, string bufferSuffix, string label, float maxLabelWidth)
        {
            const float sliderWidth = 300f;
            const float sliderTextFieldWidth = 70f;

            float oldValue = value;
            string bufferKey = key + bufferSuffix;

            // Calculate layout
            float rowHeight = Text.LineHeight;
            float totalWidth = maxLabelWidth + sliderWidth + (_showTextField ? sliderTextFieldWidth : 0f);

            Rect rowRect = listing.GetRect(rowHeight);
            Rect centeredRect = rowRect.MiddlePartPixels(totalWidth, rowHeight);

            // Position components
            Rect labelRect = centeredRect.LeftPartPixels(maxLabelWidth);
            Rect sliderRect = new(labelRect.xMax, centeredRect.y, sliderWidth, centeredRect.height);
            Rect textRect = _showTextField ?
                new Rect(sliderRect.xMax, centeredRect.y, sliderTextFieldWidth, centeredRect.height) :
                default;

            // Draw components
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.MiddleCenter;

            // Handle slider
            value = Widgets.HorizontalSlider(sliderRect, value, CommonalityMin, CommonalityMax, true, $"{value:F4}");

            // Update buffer if needed
            if (!_commonalityBuffers.ContainsKey(bufferKey) || Math.Abs(value - oldValue) > FloatTolerance4Decimals)
                _commonalityBuffers[bufferKey] = value.ToString("F4");

            // Handle text field if enabled
            if (_showTextField)
            {
                string buffer = _commonalityBuffers[bufferKey];
                Widgets.TextFieldNumeric(textRect, ref value, ref buffer, CommonalityMin, CommonalityMax);
                _commonalityBuffers[bufferKey] = buffer;
            }

            // Return whether the value changed
            return Math.Abs(value - oldValue) > FloatTolerance4Decimals;
        }
    }
}
