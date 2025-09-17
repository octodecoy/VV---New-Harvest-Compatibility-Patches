namespace NewHarvestPatches
{
    public class NewHarvestPatchesMod : Mod
    {
        public static NewHarvestPatchesModSettings Settings;
        public static NewHarvestPatchesMod Instance { get; private set; }

        public ModMetaData MetaData => Instance.Content.ModMetaData;

        public NewHarvestPatchesMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<NewHarvestPatchesModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "NHCP.ModName".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            SettingChanged = false;
            ClearBuffers();
            CloseOpenWindows();
        }

        private static void CloseOpenWindows()
        {
            // Really not necessary but whatever
            var openWindows = Find.WindowStack.Windows
                .Where(w => w is ISettingsMenuWindows)
                .ToList();

            foreach (var window in openWindows)
            {
                Find.WindowStack.TryRemove(window);
            }
        }
    }
}
