namespace NewHarvestPatches
{
    internal static class DryadUISorter
    {
        /// <summary>
        /// Try to sort dryads in UI so that our added dryads fit, and we don't have to hope for the best with their placement via xml.
        /// </summary>

        internal static void TrySortDryads()
        {
            StartStopwatch(nameof(DryadUISorter), nameof(TrySortDryads));
            try
            {
                SortDryads();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(DryadUISorter), nameof(TrySortDryads));
            }
        }

        private static void SortDryads()
        {
            // Easier to just sort all than to find first empty spots
            var defs = DefDatabase<GauranlenTreeModeDef>.AllDefsListForReading;
            if (defs.NullOrEmpty())
            {
                ToLog("No dryads found to sort.", 2);
                return;
            }

            const float spacing = 0.1665f; // step
            const int rowsPerColumn = 7;   // 0-6 then bottom at 1.0

            for (int i = 0; i < defs.Count; i++)
            {
                int col = i / rowsPerColumn;
                int row = i % rowsPerColumn;

                float y = row == rowsPerColumn - 1 ? 1f : row * spacing;
                float x = col;

                defs[i].drawPosition = new Vector2(x, y);

                ToLog($"Assigned dryad [{defs[i].defName}] -> drawPosition ({x}, {y:0.###})");
            }
        }
    }
}