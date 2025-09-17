namespace NewHarvestPatches
{
    [DefOf]
    public static class NHCP_ThingCategoryDefOf
    {
        public static ThingCategoryDef VV_NHCP_DummyCategory_AnimalFoods;
        public static ThingCategoryDef VV_NHCP_DummyCategory_Fruit;
        public static ThingCategoryDef VV_NHCP_DummyCategory_Grains;
        public static ThingCategoryDef VV_NHCP_DummyCategory_Nuts;
        public static ThingCategoryDef VV_NHCP_DummyCategory_Vegetables;    

        static NHCP_ThingCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NHCP_ThingCategoryDefOf));
        }
    }
}