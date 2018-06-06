using Harmony;
using System.Reflection;

namespace AlwaysStack
{
    internal class ModuleInitializer : Partiality.Modloader.PartialityMod
    {
        public override void Init()
        {
            FileLog.Log("Patching with " + typeof(ModuleInitializer).FullName);

            try
            {
                var harmony = HarmonyInstance.Create("cultistsimulatoruimod.alwaysstack");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                FileLog.Log("Working! " + typeof(ModuleInitializer).FullName);
            }
            catch (System.Exception e)
            {
                FileLog.Log(e.ToString());
            }
        }
    }
}