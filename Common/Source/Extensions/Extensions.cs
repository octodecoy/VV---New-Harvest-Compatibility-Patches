namespace NewHarvestPatches
{
    public static class Extensions
    {
        // Could just scribe full values or something, but whatever
        public static bool EqualsColor(this Color a, Color b, float tolerance)
        {
            return Math.Abs(a.r - b.r) < tolerance &&
                   Math.Abs(a.g - b.g) < tolerance &&
                   Math.Abs(a.b - b.b) < tolerance;
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(toCheck))
                return false;

            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsBetween<T>(this T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        // endsWith: "Tree" and !reverseEnd for trees, endsWith: "Tree" and reverseEnd for plants, endsWith: "" for both, startsWith: "" and endsWith: "" for all IsPlant
        public static bool IsPlant(this ThingDef def, string startsWith, string endsWith, bool reverseEnd = false)
        {
            return def?.defName?.StartsAndEndsWith(start: startsWith, end: endsWith, reverseEnd: reverseEnd) == true && def.IsPlant;
        }

        // startsWith: "" for all things with commonality
        public static bool HasCommonality(this ThingDef def, string contains)
        {
            return def?.defName?.Contains(contains) == true && def.stuffProps?.commonality >= 0;
        }

        public static bool IsAnimalFood(this ThingDef def, string startsWith)
        {
            var ingestible = def?.ingestible;
            return ingestible != null && 
                   def.defName?.StartsWith(startsWith) == true &&
                   (ingestible.foodType.HasFlag(FoodTypeFlags.Kibble) ||
                    ingestible.optimalityOffsetFeedingAnimals > 0) &&
                   ((int)ingestible.preferability).IsBetween(3, 4); // DesperateOnlyForHumanlikes & RawBad

        }

        public static bool StartsAndEndsWith(this string str, string start, string end, bool reverseStart = false, bool reverseEnd = false)
        {
            return (reverseStart ? !str.StartsWith(start) : str.StartsWith(start)) &&
                   (reverseEnd ? !str.EndsWith(end) : str.EndsWith(end));
        }

        // var filtered = dict.RemoveNulls<ThingDef, object, object>(includeValues: true, includeKeys: true);
        public static Dictionary<TKey, TValue> RemoveNulls<T, TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            bool includeValues,
            bool includeKeys = true)
            where T : Def
        {
            if (!includeKeys && !includeValues)
                return dictionary ?? [];

            return !dictionary.NullOrEmpty()
                ? dictionary
                    .Where(kvp =>
                        (!includeKeys ||
                            (kvp.Key is string keyStr
                                ? !string.IsNullOrWhiteSpace(keyStr) && DefDatabase<T>.GetNamedSilentFail(keyStr) != null
                                : kvp.Key is T keyDef
                                    ? keyDef != null && DefDatabase<T>.GetNamedSilentFail(keyDef.defName) != null
                                    : kvp.Key != null
                            )
                        ) &&
                        (!includeValues ||
                            (kvp.Value is string valueStr
                                ? !string.IsNullOrWhiteSpace(valueStr) && DefDatabase<T>.GetNamedSilentFail(valueStr) != null
                                : kvp.Value is T valueDef
                                    ? valueDef != null && DefDatabase<T>.GetNamedSilentFail(valueDef.defName) != null
                                    : kvp.Value != null
                            )
                        )
                    )
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                : [];
        }
    }
}