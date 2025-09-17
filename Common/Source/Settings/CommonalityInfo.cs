namespace NewHarvestPatches
{
    public class CommonalityInfo : IExposable
    {
        public string DefLabel = nameof(DefLabel);
        public float DefaultCommonality = 0f;
        public float CoreCommonality = -1f;
        public float ApparelOffset = -1f;
        public float StructureOffset = -1f;
        public float WeaponOffset = -1f;
        public void ExposeData()
        {
            Scribe_Values.Look(ref CoreCommonality, nameof(CoreCommonality), -1f, false);
            Scribe_Values.Look(ref StructureOffset, nameof(StructureOffset), -1f, false);
            Scribe_Values.Look(ref WeaponOffset, nameof(WeaponOffset), -1f, false);
            Scribe_Values.Look(ref ApparelOffset, nameof(ApparelOffset), -1f, false);
        }

        internal static void BuildCommonalityStats(ref Dictionary<string, CommonalityInfo> commonalityInfo)
        {
            if (!HasIndustrialModule)
                return;

            commonalityInfo = commonalityInfo != null
                    ? commonalityInfo.RemoveNulls<ThingDef, string, CommonalityInfo>(includeValues: false, includeKeys: true)
                    : [];

            var dictionary = IndustrialResourceDefDictionary;
            if (dictionary.NullOrEmpty())
            {
                ToLog($"Could not get defs for commonality dictionary.", 2);
                commonalityInfo.Clear();
                return;
            }

            foreach (var kvp in dictionary)
            {
                var def = kvp.Key;
                if (def.stuffProps?.commonality is not float commonality)
                    continue;

                var defName = def.defName;
                if (commonalityInfo.TryGetValue(defName, out var existingInfo))
                {
                    existingInfo.DefLabel = def.label;
                    existingInfo.DefaultCommonality = commonality;
                    float initial = -1;
                    if (ShowVEFCommonalitySettings && existingInfo.ApparelOffset == initial)
                    {
                        existingInfo.CoreCommonality = initial;
                        existingInfo.ApparelOffset = commonality;
                        existingInfo.StructureOffset = commonality;
                        existingInfo.WeaponOffset = commonality;
                    }
                    else if (!ShowVEFCommonalitySettings && existingInfo.ApparelOffset > initial)
                    {
                        existingInfo.CoreCommonality = commonality;
                        existingInfo.ApparelOffset = initial;
                        existingInfo.StructureOffset = initial;
                        existingInfo.WeaponOffset = initial;
                    }
                    continue;
                }

                (float, float) initialValues = (-1f, -1f);
                if (!ShowVEFCommonalitySettings)
                {
                    initialValues.Item1 = commonality;
                }
                else
                {
                    initialValues.Item2 = commonality;
                }

                var info = new CommonalityInfo
                {
                    DefLabel = def.label,
                    DefaultCommonality = commonality,
                    CoreCommonality = initialValues.Item1,
                    ApparelOffset = initialValues.Item2,
                    StructureOffset = initialValues.Item2,
                    WeaponOffset = initialValues.Item2
                };
                commonalityInfo[defName] = info;
            }
        }
    }
}