using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace NewHarvestPatches
{
    public static class Utils
    {
        public static class Logger
        {
            private static bool LoggingEnabled => Settings.Logging;
            private const string Prefix = "[NewHarvestPatches] - ";

            public static void ToLog(string message, int severity = 0, [CallerFilePath] string filePath = null, [CallerMemberName] string caller = null)
            {
                if (!LoggingEnabled)
                    return;

                string className = filePath != null ? System.IO.Path.GetFileNameWithoutExtension(filePath) : "UnknownClass";
                string m = $"{Prefix}Caller: [{className}.{caller}] - {message}";
                if (severity == 1)
                    Log.Warning(m);
                else if (severity == 2)
                    Log.Error(m);
                else
                    Log.Message(m);
            }

            public static void ExToLog(Exception exception, MethodBase method, string optMsg = null)
            {
                string omsg = optMsg != null ? $"(Additional info: {optMsg})\n" : "";
                string m = $"{Prefix}{omsg}Exception in {method.DeclaringType.FullName}.{method.Name}: {exception.Message}\n{exception.StackTrace}";
                Log.Error(m);
            }

            private static ConcurrentDictionary<string, Stopwatch> _stopwatches = new();
            public static double TimeElapsed;

            public static double StartStopwatch(string callerClass, string callerMethod)
            {
                if (!LoggingEnabled)
                    return 0;

                string key = $"{callerClass}.{callerMethod}";
                double previousElapsed = 0;

                // Stop and remove any existing stopwatch for this method
                if (_stopwatches.TryRemove(key, out var existing))
                {
                    existing.Stop();
                    previousElapsed = existing.Elapsed.TotalSeconds;
                    TimeElapsed += previousElapsed;
                    ToLog($"{key} (previous run) completed in: {previousElapsed:F3} seconds.", 0);
                }

                var sw = Stopwatch.StartNew();
                _stopwatches[key] = sw;
                return previousElapsed;
            }

            public static double LogStopwatch(string callerClass, string callerMethod)
            {
                if (!LoggingEnabled)
                    return 0;

                string key = $"{callerClass}.{callerMethod}";

                if (_stopwatches.TryRemove(key, out var sw))
                {
                    sw.Stop();
                    var elapsed = sw.Elapsed.TotalSeconds;
                    TimeElapsed += elapsed;
                    ToLog($"{key} completed in: {elapsed:F3} seconds.", 0);
                    return elapsed;
                }
                else
                {
                    ToLog($"{key} completed but stopwatch was not running.", 0);
                    return 0;
                }
            }

            public static void LogInitTime()
            {
                foreach (var sw in _stopwatches.Values)
                {
                    sw.Stop();
                }

                _stopwatches = null;

                ToLog($"Finished initializing in {TimeElapsed:F3} seconds.", 0);
                TimeElapsed = 0;
            }
        }

        public static class VersionChecker
        {
            public static bool HasCurrentGameVersion = HasOdyssey || IsGameVersionAtLeast(new Version("1.6"));
            /// <summary>
            /// Version from the mod's About.xml.
            /// </summary>
            public static Version ModVersion = GetVersion();
            public static string[] PackageIDs
            {
                get
                {
                    var ids = new List<string>();
                    if (HasMainModule) ids.Add("vvenchov.vvnewharvest");
                    if (HasForageModule) ids.Add("vvenchov.vvnewharvestforagecrops");
                    if (HasGardenModule) ids.Add("vvenchov.vvnewharvestgardencrops");
                    if (HasIndustrialModule) ids.Add("vvenchov.vvnewharvestindustrialcrops");
                    if (HasMedicinalModule) ids.Add("vvenchov.vvnewharvestmedicinalplants");
                    if (HasTreesModule) ids.Add("vvenchov.vvnewharvesttrees");
                    return [.. ids];
                }
            }
            /// <summary>
            /// Dictionary of active New Harvest modules and their versions from their About.xml.
            /// </summary>
            /// 
            public static Dictionary<string, (string version, string translationKey)> NewHarvestVersions => GetModVersions(PackageIDs);

            public static bool IsGameVersionAtLeast(Version version, bool checkBuild = false, bool checkRev = false)
            {
                if (version == null) return false;

                // Build a version to compare against, depending on which parts we care about
                int build = checkBuild ? VersionControl.CurrentBuild : 0;
                int revision = checkRev ? VersionControl.CurrentRevision : 0;

                Version current = new(VersionControl.CurrentMajor, VersionControl.CurrentMinor, build, revision);

                return current.CompareTo(version) >= 0;
            }



            public static Dictionary<string, (string version, string translationKey)> GetModVersions(params string[] packageIDs)
            {
                if (packageIDs.Length == 0)
                    return [];

                string[] moduleNames = [.. Enum.GetValues(typeof(Module)).Cast<Module>().Select(m => m.ToString())];

                var dictionary = new Dictionary<string, (string, string)>();
                foreach (var packageID in packageIDs)
                {
                    var mod = ModLister.GetActiveModWithIdentifier(packageID);
                    if (mod != null)
                    {
                        string translationKey = null;
                        foreach (var moduleName in moduleNames)
                        {
                            if (packageID.ContainsIgnoreCase(moduleName))
                            {
                                translationKey = moduleName;
                                break;
                            }
                        }

                        string modVersion = mod.ModVersion ?? "??";
                        translationKey ??= HasMainModule ? $"{Module.Full}" : "??";
                        dictionary[mod.Name] = (modVersion, translationKey);

                        // If main is installed, the others won't be, or shouldn't be
                        if (translationKey == $"{Module.Full}")
                        {
                            return dictionary;
                        }
                    }
                }
                return dictionary;
            }

            private static Version GetVersion()
            {
                string versionString = NewHarvestPatchesMod.Instance?.MetaData?.ModVersion;
                return string.IsNullOrWhiteSpace(versionString) ? null : new Version(versionString);
            }

            internal static (Version oldVersion, Version newVersion) UpdateModVersion()
            {
                try
                {
                    Version currentVersion = ModVersion;
                    if (currentVersion == null)
                    {
                        ToLog("Could not get version.", 2);
                        return (null, null);
                    }

                    string savedVersionString = Settings.ModVersion;

                    Version savedVersion = null;
                    bool update = false;
                    if (string.IsNullOrWhiteSpace(savedVersionString))
                    {
                        ToLog($"No saved version found, setting to version {currentVersion.Major}.{currentVersion.Minor}.", 0);
                        update = true;
                    }
                    else
                    {
                        savedVersion = new(savedVersionString);
                        bool majorMinorEqual = savedVersion.Major == currentVersion.Major && savedVersion.Minor == currentVersion.Minor;
                        if (!majorMinorEqual)
                        {
                            update = true;
                            if (savedVersion.Major < currentVersion.Major || (savedVersion.Major == currentVersion.Major && savedVersion.Minor < currentVersion.Minor))
                            {
                                ToLog($"Updated from version [{savedVersion.Major}.{savedVersion.Minor}] to version [{currentVersion.Major}.{currentVersion.Minor}].", 0);
                            }
                            else
                            {
                                ToLog($"Downgraded from version [{savedVersion.Major}.{savedVersion.Minor}] to version [{currentVersion.Major}.{currentVersion.Minor}]. This is unexpected.", 1);
                            }
                        }
                    }
                    if (update)
                    {
                        Settings.UpdateScribedVersion(currentVersion);
                    }
                    return (savedVersion, currentVersion);
                }
                catch (Exception ex)
                {
                    ExToLog(ex, MethodBase.GetCurrentMethod());
                    return (null, null);
                }
            }
        }

        public static class SettingChecker
        {
            public static NewHarvestPatchesModSettings Settings => NewHarvestPatchesMod.Settings;

            public static IEnumerable<string> EnabledSettings => Settings.EnabledSettings;
        }

        public static class ModChecker
        {
            public static bool HasMainModule = IsModActive("vvenchov.vvnewharvest");
            public static bool HasForageModule = HasMainModule || IsModActive("vvenchov.vvnewharvestforagecrops");
            public static bool HasGardenModule = HasMainModule || IsModActive("vvenchov.vvnewharvestgardencrops");
            public static bool HasIndustrialModule = HasMainModule || IsModActive("vvenchov.vvnewharvestindustrialcrops");
            public static bool HasMedicinalModule = HasMainModule || IsModActive("vvenchov.vvnewharvestmedicinalplants");
            public static bool HasTreesModule = HasMainModule || IsModActive("vvenchov.vvnewharvesttrees");
            public static bool HasAnyModule = HasForageModule || HasGardenModule || HasIndustrialModule || HasMedicinalModule || HasTreesModule;
            public static bool HasOdyssey = IsModActive("ludeon.rimworld.odyssey");
            public static bool HasIdeology = IsModActive("ludeon.rimworld.ideology");
            public static bool HasMedievalOverhaul = IsModActive("dankpyon.medieval.overhaul");

            /// <summary>
            /// If installed, show setting to move Medicinal module drinks to its non-alcoholic category.
            /// </summary>
            public static bool HasVanillaBrewingExpanded = IsModActive("vanillaexpanded.vbrewe");

            /// <summary>
            /// If Ferny's Floor Menu is active, we don't need to show our floor dropdown settings, as it makes dropdowns obsolete.
            /// </summary>
            public static bool HasFernyFloorMenu = IsModActive("ferny.floormenu");

            /// <summary>
            /// If Vanilla Expanded Framework is active, we can show commonality sliders utlizing its StuffExtension class instead of base game commonality stat.
            /// </summary>
            public static bool HasVanillaExpandedFramework = IsModActive("oskarpotocki.vanillafactionsexpanded.core");

            /// <summary>
            /// If Better Sliders is active, we don't need to place text fields next to our sliders.
            /// </summary>
            public static bool HasBetterSliders = IsModActive("sirrandoo.bettersliders");


            /// <summary>
            /// Indicates whether any tree modules are installed.
            /// </summary>
            /// <remarks>Returns <see langword="true"/>If HasIndustrialModule or HasMedicinalModule or HasTreesModule.</remarks>
            public static bool HasAnyTrees = HasIndustrialModule || HasMedicinalModule || HasTreesModule;

            /// <summary>
            /// Indicates whether any modules that provide fruit are installed.
            /// </summary>
            /// <remarks>Returns <see langword="true"/>If HasGardenModule or HasMedicinalModule or HasTreesModule.</remarks>
            public static bool HasAnyFruit = HasGardenModule || HasMedicinalModule || HasTreesModule;

            /// <summary>
            /// Indicates whether any modules that provide vegetables are installed.
            /// </summary>
            /// <remarks>Returns <see langword="true"/>If HasGardenModule or HasMedicinalModule.</remarks>
            public static bool HasAnyVegetables = HasGardenModule || HasMedicinalModule;

            /// <summary>
            /// Indicates whether the fuel settings should be displayed in menu.
            /// </summary>
            /// <remarks>If !HasIndustrialModule or HasMedievalOverhaul or LWM's Fuel Filter is active this returns <see langword="false"/>.
            /// Both mods add a fuel ITab, so user can just toggle fuels themselves.
            /// </remarks>
            public static bool ShowFuelSettings = HasIndustrialModule && !HasMedievalOverhaul && !IsModActive("zal.lwmfuelfilter");

            /// <summary>
            /// Indicates whether the Wood Conversion Recipe setting should be displayed in menu.
            /// </summary>
            /// <remarks>If Medieval Overhaul, Expanded Woodworking or Extended Woodworking are active this returns <see langword="false"/>.
            /// All 3 mods add recipes to convert wood types, and it's too much hassle to change our recipe's product just to fit.
            /// </remarks>
            public static bool ShowWoodConvertRecipe = !HasMedievalOverhaul && !IsAnyModActive(ignorePostfix: true, "teflonjim.extendedwoodworking", "zal.expandwoodwork");

            /// <summary>
            /// Indicates whether Vanilla Expanded Framework's commonality Stuff Extension sliders should be displayed in menu.
            /// </summary>
            /// <remarks>If !HasIndustrialModule or !HasVanillaExpandedFramework, returns <see langword="false"/>.</remarks>
            public static bool ShowVEFCommonalitySettings = HasIndustrialModule && HasVanillaExpandedFramework;

            public static bool IsModActive(string packageId, bool ignorePostfix = true)
            {
                return !string.IsNullOrWhiteSpace(packageId) && ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix) != null;
            }

            public static bool IsAnyModActive(bool ignorePostfix = true, params string[] packageIds)
            {
                if (packageIds.NullOrEmpty())
                    return false;

                if (ignorePostfix)
                    return ModLister.AnyModActiveNoSuffix([.. packageIds]);
                else
                    return ModLister.AnyFromListActive([.. packageIds]);
            }
        }
    }
}