namespace NewHarvestPatches
{
    internal static class FallColorDisabler
    {
        /// <summary>
        /// Turn off the fall color shader parameter for all trees in the DeciduousTrees list in settings using Reflection.
        /// </summary>

        internal static void TryDisableFallColors()
        {
            StartStopwatch(nameof(FallColorDisabler), nameof(TryDisableFallColors));
            try
            {
                DisableFallColors();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(FallColorDisabler), nameof(TryDisableFallColors));
            }
        }

        private static void DisableFallColors()
        { 
            //var treesDefNames = EnabledSettings
            //    .Where(s => s.StartsWith(Setting.Prefix.NoFallColors_))
            //    .Select(s => s.Substring(Setting.Prefix.NoFallColors_.Length))
            //    .ToList();

            var enabledDefNames = ExtractNamesFromEnabledSettings(Setting.Prefix.NoFallColors_);
            if (enabledDefNames.NullOrEmpty())
                return;

            var nameField = typeof(ShaderParameter).GetField("name", BindingFlags.NonPublic | BindingFlags.Instance);
            if (nameField == null)
            {
                ToLog("Could not access ShaderParameter.name field via reflection.", 2);
                return;
            }

            var valueField = typeof(ShaderParameter).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
            if (valueField == null)
            {
                ToLog("Could not access ShaderParameter.value field via reflection.", 2);
                return;
            }

            const string targetName = "_FallBehaviorEnabled";
            foreach (var treeDefName in enabledDefNames)
            {
                var treeDef = DefDatabase<ThingDef>.GetNamedSilentFail(treeDefName);
                if (treeDef == null)
                {
                    ToLog($"Couldn't find ThingDef named {treeDefName}. Skipping.", 1);
                    continue;
                }

                ShaderParameter param = treeDef.graphicData?.shaderParameters?.FirstOrDefault(p =>
                {
                    return nameField != null && (string)nameField.GetValue(p) == targetName;
                });

                if (param == null)
                {
                    ToLog($"Couldn't find shader parameter named {targetName} for tree {treeDefName}. Skipping.", 1);
                    continue;
                }

                Vector4 value = valueField != null ? (Vector4)valueField.GetValue(param) : default;

                if (value.x != 1f)
                {
                    ToLog($"Shader parameter {targetName} for tree {treeDefName} is already disabled (value.x = {value.x}). Skipping.", 1);
                    continue;
                }

                ToLog($"Disabling fall color shader parameter {targetName} for tree {treeDefName}");
                valueField.SetValue(param, Vector4.zero); // new Vector4(0f, 0f, 0f, 0f)
            }
        }
    }
}