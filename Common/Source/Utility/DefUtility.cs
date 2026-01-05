using RimWorld;

namespace NewHarvestPatches
{
    internal static class DefUtility
    {
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
    }
}