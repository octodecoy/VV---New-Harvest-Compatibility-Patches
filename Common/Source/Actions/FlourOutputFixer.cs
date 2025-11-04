namespace NewHarvestPatches
{
    internal static class FlourOutputFixer
    {
        internal static void TryFixFlourOutput()
        {
            StartStopwatch(nameof(FlourOutputFixer), nameof(TryFixFlourOutput));
            try
            {
                FixFlourOutput();
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod());
            }
            finally
            {
                LogStopwatch(nameof(FlourOutputFixer), nameof(TryFixFlourOutput));
            }
        }

        private static void FixFlourOutput()
        {
            List<ThingDef> grainPlants = [.. DefDatabase<ThingDef>.AllDefsListForReading
                .Where(td => td?.defName?.StartsWith(ModName.Prefix.VV_) == true &&
                             td.IsPlant == true &&
                             td.plant?.harvestedThingDef is ThingDef harvestedDef &&
                             harvestedDef.defName?.StartsAndEndsWith(start: ModName.Prefix.VV_, end: "Grain") == true)
                ];

            if (grainPlants.Count == 0)
            {
                ToLog("No grain plant ThingDefs found to fix flour output.", 2);
                return;
            }

            foreach (var plantDef in grainPlants)
            {
                var modExtension = plantDef.GetModExtension<VEF.Plants.DualCropExtension>();
                if (modExtension == null)
                {
                    ToLog($"Plant [{plantDef.defName}] missing VEF.Plants.DualCropExtension ModExtension, skipping flour output fix.", 2);
                    continue;
                }

                int yield = (int)plantDef.plant.harvestYield;
                if (yield == 0)
                    continue;

                int desiredOutputAmount = yield / 2;
                int currentOutputAmount = modExtension.outPutAmount;
                if (desiredOutputAmount != currentOutputAmount)
                {
                    modExtension.outPutAmount = desiredOutputAmount;
                    ToLog($"Fixed flour output for plant [{plantDef.defName}] from [{currentOutputAmount}] to [{desiredOutputAmount}].");
                }
            }
        }
    }
}
