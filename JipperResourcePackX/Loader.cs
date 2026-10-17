#if ML
using MelonLoader;

[assembly: MelonInfo(typeof(JipperResourcePackX.Loader), "JipperResourcePackX", "1.6.0", "Jongyeol & Kkitut")]
[assembly: MelonGame("7th Beat Games", "A Dance of Fire and Ice")]

namespace JipperResourcePackX {
    public class Loader : MelonMod {
        public static Loader Instance { get; private set; }

        public override void OnInitializeMelon() {
            Instance = this;
            if (!Main.InitializeML()) {
                ModLogger.Error("Failed to initialize JipperResourcePackX. Aborting mod load.");
                return;
            }
        }

        public override void OnDeinitializeMelon() {
            Main.Dispose();
            Instance = null;
        }
    }
}
#endif
