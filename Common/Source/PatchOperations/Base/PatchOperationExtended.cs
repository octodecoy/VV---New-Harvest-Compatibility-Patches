namespace NewHarvestPatches
{
    internal abstract class PatchOperationExtended : PatchOperation
    {
        protected readonly Logic logic = Logic.Or;
        protected readonly PatchOperation caseTrue = null;
        protected readonly PatchOperation caseFalse = null;
        protected static bool EvaluateLogic<T>(Logic logic, IEnumerable<T> source, Func<T, bool> predicate)
        {
            return logic switch
            {
                Logic.Or => source.Any(predicate),
                Logic.And => source.All(predicate),
                Logic.Not => !source.Any(predicate),
                Logic.Xor => source.Count(predicate) == 1,
                _ => false
            };
        }
    }
}