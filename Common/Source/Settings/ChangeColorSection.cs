using Verse.Sound;

namespace NewHarvestPatches
{
    internal static class ChangeColorSection
    {
        public static string _selectedMaterialDefName = null;
        public static readonly Dictionary<string, string> _colorChannelBuffers = [];
        private static readonly Dictionary<string, (bool doStuff, bool doThing, Color stuffColor, Color thingColor)> _originalColorStates = [];
        private static readonly Color _inactiveColor = new(0.37f, 0.37f, 0.37f, 0.8f);

        public static float DoColorDropdowns(Listing_Standard ls, Dictionary<string, ColorInfo> materialColors)
        {
            var originalUIState = new UIState(GUI.color, Text.Anchor, Text.Font);

            string previousMaterial = _selectedMaterialDefName;
            _selectedMaterialDefName ??= materialColors.Keys.First();
            var info = materialColors[_selectedMaterialDefName];

            if (info.DefForIcon == null)
            {
                return ls.CurHeight;
            }

            // Store original state for checking changes
            if (previousMaterial != _selectedMaterialDefName ||
                !_originalColorStates.ContainsKey(_selectedMaterialDefName))
            {
                _originalColorStates[_selectedMaterialDefName] = (
                    info.DoStuff,
                    info.DoThing,
                    info.NewStuffColor,
                    info.NewThingColor
                );
            }

            // Constants
            const float iconSize = 72f;
            const float dropdownHeight = 32f;
            const float mainRectHeight = 160f;
            const float spacing = 12f;

            // Main layout areas
            Rect mainRect = ls.GetRect(mainRectHeight);
            Rect leftRect = mainRect.LeftPart(0.25f);
            Rect rightRect = mainRect.RightPart(0.25f);
            Rect middleRect = mainRect.MiddlePart(0.5f, 1.0f);

            // Draw radio buttons in left area
            DrawRadioButtons(leftRect, ref info);

            // Draw swatch and icon in middle area
            DrawSwatchAndIcon(middleRect, info, iconSize, spacing);

            // Draw dropdown under previews
            DrawMaterialDropdown(middleRect, dropdownHeight, info, materialColors);

            // Draw RGB controls in right area
            DrawColorControls(rightRect, info);

            originalUIState.Restore();
            ls.Gap(GenUI.GapWide);

            return ls.CurHeight;
        }

        private static void DrawRadioButtons(Rect rect, ref ColorInfo info)
        {
            string[] radioLabels = [
                Translator.TranslateKey(TKey.Type.RadioLabel, "ApplyToStuff"),
                Translator.TranslateKey(TKey.Type.RadioLabel, "ApplyToThing"),
                Translator.TranslateKey(TKey.Type.RadioLabel, "ApplyToBoth")
            ];

            // Map current settings to radio button index
            int mode = (info.DoStuff, info.DoThing) switch
            {
                (true, false) => 0,  // Stuff
                (false, true) => 1,  // Thing
                (true, true) => 2,   // Both
                _ => 0               // Default to Stuff
            };

            const float radioButtonHeight = 28f;
            const float radioButtonSpacing = 2f;
            float totalRadioHeight = 3 * radioButtonHeight + 2 * radioButtonSpacing;
            float startY = rect.y + (rect.height - totalRadioHeight) / 2f; // Center vertically

            Text.Font = GameFont.Tiny;
            for (int i = 0; i < 3; i++)
            {
                Rect radioRect = new(rect.x, startY + i * (radioButtonHeight + radioButtonSpacing), rect.width, radioButtonHeight);
                bool selected = mode == i;

                if (Widgets.RadioButtonLabeled(radioRect, radioLabels[i], selected))
                {
                    switch (i)
                    {
                        case 0: info.DoStuff = true; info.DoThing = false; break;
                        case 1: info.DoStuff = false; info.DoThing = true; break;
                        case 2: info.DoStuff = true; info.DoThing = true; break;
                    }
                    SettingChanged = true;
                }
            }
            Text.Font = GameFont.Small;
        }

        private static void DrawSwatchAndIcon(Rect rect, ColorInfo info, float iconSize, float spacing)
        {
            float totalWidth = iconSize * 2 + spacing;
            float startX = rect.x + (rect.width - totalWidth) / 2f;
            float centerY = rect.y + (rect.height - iconSize) / 2f;

            // Labels
            float labelHeight = Text.LineHeight;
            Rect swatchLabelRect = new(startX, centerY - labelHeight, iconSize, labelHeight);
            Rect iconLabelRect = new(startX + iconSize + spacing, centerY - labelHeight, iconSize, labelHeight);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(swatchLabelRect, Translator.TranslateKey(TKey.Type.General, "Stuff"));
            Widgets.Label(iconLabelRect, Translator.TranslateKey(TKey.Type.General, "Thing"));

            // Swatch
            Rect swatchRect = new(startX, centerY, iconSize, iconSize);
            GUI.color = info.DoStuff ? info.NewStuffColor : info.DefaultStuffColor;
            GUI.DrawTexture(swatchRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            // Icon
            Rect iconRect = new(startX + iconSize + spacing, centerY, iconSize, iconSize);
            Widgets.DefIcon(iconRect, info.DefForIcon, color: info.DoThing ? info.NewThingColor : info.DefaultThingColor);
        }

        private static void DrawMaterialDropdown(Rect rect, float dropdownHeight, ColorInfo info, Dictionary<string, ColorInfo> materialColors)
        {
            const float dropdownWidth = 160f;
            float dropdownX = rect.x + (rect.width - dropdownWidth) / 2f;
            float dropdownY = rect.y + rect.height - dropdownHeight - 8f;
            Rect dropdownRect = new(dropdownX, dropdownY, dropdownWidth, dropdownHeight);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Dropdown(
                dropdownRect,
                _selectedMaterialDefName,
                key => key,
                key => materialColors.Keys.Select(defName =>
                    new Widgets.DropdownMenuElement<string>
                    {
                        option = new FloatMenuOption(
                            materialColors[defName].DefLabel,
                            () => _selectedMaterialDefName = defName
                        ),
                        payload = defName
                    }
                ),
                buttonLabel: info.DefLabel
            );
        }

        private static void DrawColorControls(Rect rect, ColorInfo info)
        {
            const float labelWidth = 18f;
            const float textBoxWidth = 60f;
            const float textBoxHeight = 28f;
            const float spacing = 12f;
            const float buttonHeight = textBoxHeight;
            const int numFields = 3;

            // Calculate layout
            float totalFieldsHeight = numFields * (textBoxHeight + spacing) - spacing;
            float totalHeight = totalFieldsHeight + spacing + buttonHeight;
            float startY = rect.y + (rect.height - totalHeight) / 2f;

            // Get the active color based on current mode (stuff vs thing)
            Color activeSourceColor = info.DoStuff ? info.NewStuffColor : info.DefaultStuffColor;
            if (info.DoThing && !info.DoStuff)
                activeSourceColor = info.NewThingColor;

            string[] channelNames = ["R", "G", "B"];
            int[] channelValues255 = [
                (int)Mathf.Round(activeSourceColor.r * 255f),
                (int)Mathf.Round(activeSourceColor.g * 255f),
                (int)Mathf.Round(activeSourceColor.b * 255f)
            ];

            int[] newChannelValues255 = new int[3];
            bool hasColorChanges = false;

            // Draw RGB input fields
            for (int i = 0; i < 3; i++)
            {
                float rowY = startY + i * (textBoxHeight + spacing);
                string bufferKey = $"{_selectedMaterialDefName}_{channelNames[i]}";

                if (!_colorChannelBuffers.ContainsKey(bufferKey))
                    _colorChannelBuffers[bufferKey] = channelValues255[i].ToString();

                // Channel label
                Rect labelRect = new(rect.x, rowY, labelWidth, textBoxHeight);
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(labelRect, channelNames[i]);

                // Numeric input field
                Rect boxRect = new(rect.x + labelWidth + 4f, rowY, textBoxWidth, textBoxHeight);
                string buffer = _colorChannelBuffers[bufferKey];
                int value255 = channelValues255[i];
                Widgets.TextFieldNumeric(boxRect, ref value255, ref buffer, 0, 255);
                _colorChannelBuffers[bufferKey] = buffer;

                // Parse and store the value
                newChannelValues255[i] = int.TryParse(buffer, out int parsed)
                    ? Mathf.Clamp(parsed, 0, 255)
                    : value255;

                // Check if this channel has changed from the source value
                if (newChannelValues255[i] != channelValues255[i])
                    hasColorChanges = true;

                // Show normalized value (0-1)
                float normalized = newChannelValues255[i] / 255f;
                Rect normLabelRect = new(boxRect.xMax + 8f, rowY, 48f, textBoxHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(normLabelRect, normalized.ToString("F3"));
            }

            // Get the original state for comparison
            var (doStuff, doThing, stuffColor, thingColor) = _originalColorStates.TryGetValue(_selectedMaterialDefName, out var state)
                ? state
                : (doStuff: info.DoStuff, doThing: info.DoThing, stuffColor: info.NewStuffColor, thingColor: info.NewThingColor);

            // Check if radio buttons changed from original state
            bool radioChanged = (info.DoStuff != doStuff) ||
                                (info.DoThing != doThing);

            // Check if colors have changed from original values
            bool colorStateChanged = false;
            if (!hasColorChanges) // Only check if textfield values haven't changed
            {
                //if (info.DoStuff && !ColorsEqual(info.NewStuffColor, stuffColor))
                if (info.DoStuff && !info.NewStuffColor.EqualsColor(stuffColor, 0.001f))
                    colorStateChanged = true;

                if (info.DoThing && !info.NewThingColor.EqualsColor(thingColor, 0.001f))
                    colorStateChanged = true;
            }

            // Only enable the button if something changed
            bool shouldEnableButton = hasColorChanges || radioChanged || colorStateChanged;

            // Apply button
            float buttonX = rect.x + labelWidth;
            float buttonY = startY + totalFieldsHeight + spacing;
            Rect buttonRect = new(buttonX, buttonY, 80f, buttonHeight);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = shouldEnableButton ? Color.white : _inactiveColor;
            if (Widgets.ButtonText(buttonRect, Translator.TranslateKey(TKey.Type.Button, "Apply"), active: shouldEnableButton))
            {
                // Create new color from input values
                Color chosenColor = new ColorInt(newChannelValues255[0], newChannelValues255[1], newChannelValues255[2]).ToColor;

                // Apply to stuff if selected
                if (info.DoStuff)
                    info.NewStuffColor = chosenColor;
                else
                    info.NewStuffColor = info.DefaultStuffColor;

                // Apply to thing if selected
                if (info.DoThing)
                    info.NewThingColor = chosenColor;
                else
                    info.NewThingColor = info.DefaultThingColor;

                // Update the stored original state to reflect new values
                _originalColorStates[_selectedMaterialDefName] = (
                    info.DoStuff,
                    info.DoThing,
                    info.NewStuffColor,
                    info.NewThingColor
                );

                SoundDefOf.Click.PlayOneShotOnCamera();

                SettingChanged = true;
            }
            GUI.color = Color.white;
        }
    }
}