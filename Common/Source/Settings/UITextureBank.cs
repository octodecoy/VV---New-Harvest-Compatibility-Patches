using Verse;

namespace NewHarvestPatches
{
    // [StaticConstructorOnStartup] or else warning.
    [StaticConstructorOnStartup]
    public static class UITextureBank
    {
        //public static Texture2D UIBackgroundIcon = ContentFinder<Texture2D>.Get("NHCP/UI/MenuBackground/BackgroundLogo", false);
        public static Texture2D UIBackgroundIcon = null;

        static UITextureBank()
        {
            var textures = ContentFinder<Texture2D>.GetAllInFolder("NHCP/UI/MenuBackground");
            if (textures.EnumerableNullOrEmpty())
                return;

            UIBackgroundIcon = textures.RandomElement();
        }

        public static void DrawBackgroundLogo(Rect inRect)
        {
            if (UIBackgroundIcon == null)
                return;

            Widgets.DrawTextureFitted(inRect, UIBackgroundIcon, 0.8f, 0.08f);
        }
    }
}
