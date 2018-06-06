using Assets.Core.Interfaces;
using Assets.CS.TabletopUI;
using Harmony;
using System;
using System.Linq;

// This mod makes it so that tokens always stack with tokens of the same kind if possible.
// Specifically, newly created tokens stack, and when a token decays (including exhausted
// tokens refreshing) the result is stacked if possible.

namespace AlwaysStack
{
    static class DebugHelpers
    {
        static public void DumpObject(Object o)
        {
            var c = o.GetType();

            foreach (var f in c.GetFields())
            {
                FileLog.Log(string.Format("{0} = {1}", f.Name, f.GetValue(o).ToString()));
            }
        }
    }

    [HarmonyPatch(typeof(ElementStackToken))]
    [HarmonyPatch("ReturnToTabletop")]
    [HarmonyPatch(new Type[] { typeof(Context) })]
    class ElementStackToken_ReturnToTabletop_Patch
    {
        static bool Prefix(ElementStackToken __instance, Context context)
        {
            FileLog.Log("ElementStackToken_ReturnToTabletop_Patch v2");

            if (Registry.Retrieve<ICompendium>().GetElementById(__instance.EntityId).Unique)
                return true;

            foreach (var stack in Registry.Retrieve<TabletopManager>()._tabletop.GetElementStacksManager().GetStacks().OfType<ElementStackToken>())
            {
                // Make sure we're merging into a stack that's on the tabletop - otherwise we can lose elements forever
                if (stack.IsInAir || stack.transform.parent.GetComponent<TabletopTokenContainer>() == null)
                    continue;

                if (stack == __instance)
                    continue;

                if (__instance.CanInteractWithTokenDroppedOn(stack))
                {
                    // Manually combine stacks
                    stack.SetQuantity(stack.Quantity + __instance.Quantity);
                    __instance.Retire(false);
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ElementStackToken))]
    [HarmonyPatch("ChangeTo")]
    [HarmonyPatch(new Type[] { typeof(string) })]
    class ElementStackToken_ChangeTo_Patch
    {
        static void Postfix(ElementStackToken __instance, string elementId)
        {
            ElementStackToken token = null;
            
            // Find the newly created stack
            foreach (var stack in Registry.Retrieve<TabletopManager>()._tabletop.GetElementStacksManager().GetStacks().OfType<ElementStackToken>())
            {
                if (stack.transform.position == __instance.transform.position && stack.EntityId == elementId)
                {
                    token = stack;
                    break;
                }
            }

            if (token == null)
                return;

            foreach (var stack in Registry.Retrieve<TabletopManager>()._tabletop.GetElementStacksManager().GetStacks().OfType<ElementStackToken>())
            {
                if (token != stack && token.CanInteractWithTokenDroppedOn(stack))
                {
                    // Manually combine stacks
                    stack.SetQuantity(stack.Quantity + token.Quantity);
                    token.Retire(false);
                    FileLog.Log("__instance = " + __instance.SaveLocationInfo + " token = " + token.SaveLocationInfo + " stack = " + stack.SaveLocationInfo);
                    return;
                }
            }
        }
    }
}
