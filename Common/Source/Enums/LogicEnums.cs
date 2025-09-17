namespace NewHarvestPatches
{
    internal enum Logic
    {
        Or,
        And,
        Not, // For use in groups, no need for <mods> since we can just use caseFalse
        Xor
    }
}