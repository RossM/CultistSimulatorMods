using Harmony;
using System.Reflection;

namespace SnapToGrid
{
    internal class ModuleInitializer : Partiality.Modloader.PartialityMod
    {
        public override void Init()
        {
            //FileLog.Log("Patching with " + typeof(ModuleInitializer).FullName);

            try
            {
                var harmony = HarmonyInstance.Create("cultistsimulatoruimod.snaptogrid");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                //FileLog.Log("Working! " + typeof(ModuleInitializer).FullName);
            } 
            catch (System.Exception e)
            {
                //FileLog.Log(e.ToString());
            }
        }
    }
}