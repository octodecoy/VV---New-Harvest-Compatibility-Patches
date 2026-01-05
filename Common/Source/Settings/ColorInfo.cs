namespace NewHarvestPatches
{
    public class ColorInfo : IExposable
    {
        public string DefLabel = nameof(DefLabel);
        public ThingDef DefForIcon = null;
        public Color DefaultStuffColor = white;
        public Color NewStuffColor = white;
        public Color DefaultThingColor = white;
        public Color NewThingColor = white;
        public bool DoStuff = true;
        public bool DoThing = false;

        public void ExposeData()
        {
            Scribe_Values.Look(ref NewStuffColor, nameof(NewStuffColor), white, true);
            Scribe_Values.Look(ref DoStuff, nameof(DoStuff), true, true);
            Scribe_Values.Look(ref NewThingColor, nameof(NewThingColor), white, true);
            Scribe_Values.Look(ref DoThing, nameof(DoThing), false, true);
        }

        internal static void BuildColorInfo(ref Dictionary<string, ColorInfo> colorInfo)
        {
            if (!HasIndustrialModule)
                return;
            
            colorInfo = colorInfo != null
                    ? colorInfo.RemoveNulls<ThingDef, string, ColorInfo>(includeValues: false, includeKeys: true)
                    : [];

            var dictionary = ThingDefUtility.IndustrialResourceDefDictionary;
            if (dictionary.NullOrEmpty())
            {
                ToLog($"Could not get defs for color dictionary.", 2);
                colorInfo.Clear();
                return;
            }

            foreach (var kvp in dictionary)
            {
                var def = kvp.Key;
                if (def.stuffProps?.color is not Color stuffColor)
                    continue;

                var thingColor = def.graphicData?.color ?? white;

                var defName = def.defName;
                if (colorInfo.TryGetValue(defName, out var existingInfo))
                {
                    existingInfo.DefLabel = def.label;
                    existingInfo.DefForIcon = def;
                    existingInfo.DefaultStuffColor = stuffColor;
                    existingInfo.DefaultThingColor = thingColor;
                    continue;
                }

                colorInfo[defName] = new ColorInfo
                {
                    DefLabel = def.label,
                    DefForIcon = def,
                    DefaultStuffColor = stuffColor,
                    DefaultThingColor = thingColor,
                    NewStuffColor = stuffColor,
                    NewThingColor = thingColor
                };
            }
        }
    }
}