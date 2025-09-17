namespace NewHarvestPatches
{
    internal class ConditionalSetting : PatchOperationExtended
    {
        /// <summary>
        /// Run patches based on the value of a setting or settings.
        /// </summary>

        private readonly string setting = null;
        private readonly List<string> settings = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (GetFlag())
            {
                if (caseTrue != null)
                    return caseTrue.Apply(xml);
            }
            else if (caseFalse != null)
            {
                return caseFalse.Apply(xml);
            }
            return true;
        }

        private bool GetFlag()
        {
            var enabledSettings = EnabledSettings;
            if (enabledSettings == null)
            {
                ToLog($"Settings failed to initialize in ConditionalSetting patch operation for setting [{(!string.IsNullOrWhiteSpace(setting) ? setting : (settings != null ? string.Join(", ", settings) : "null"))}].", 2);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(setting))
            {
                return enabledSettings.Contains(setting);
            }
            else if (!settings.NullOrEmpty())
            {
                return EvaluateLogic(logic, settings, enabledSettings.Contains);
            }
            else 
            {
                ToLog("No settings supplied for ConditionalSetting patch operation.", 1);
            }
            return false;
        }
    }
}

