using Assets.CS.TabletopUI;
using Assets.TabletopUi.Scripts.Infrastructure;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// This mod makes cards on the tabletop snap to a grid.

namespace SnapToGrid
{
    public class Utility
    {
        // Step sizes. These are a half-card plus a bit of padding.
        public const float xStep = 50f;
        public const float yStep = 70f;

        public static void Snap(ref float x, ref float y)
        {
            var tabletop = Registry.Retrieve<TabletopManager>()._tabletop;

            // The location is in absolute coordinates, so adjust to be relative to the table
            x -= tabletop.transform.position.x;
            y -= tabletop.transform.position.y;

            // Round to nearest step
            x = xStep * Mathf.Round(x / xStep);
            y = yStep * Mathf.Round(y / yStep);

            // Adjust back to absolute coordinates
            x += tabletop.transform.position.x;
            y += tabletop.transform.position.y;
        }
    }

    [HarmonyPatch(typeof(Choreographer))]
    [HarmonyPatch("ArrangeTokenOnTable")]
    [HarmonyPatch(new Type[] { typeof(ElementStackToken), typeof(Context), typeof(Vector2?), typeof(bool) })]
    class Choreographer_ArrangeTokenOnTable_Patch
    {
        // This runs right after an element (card) token is placed onto the table from elsewhere
        static void Postfix(ref ElementStackToken stack, Context context, Vector2? pos = null, bool pushOthers = false)
        {
            float x = stack.RectTransform.position.x;
            float y = stack.RectTransform.position.y;
            Utility.Snap(ref x, ref y);
            stack.RectTransform.position = new Vector3(x, y, stack.RectTransform.position.z);
        }
    }

    [HarmonyPatch(typeof(TabletopTokenContainer))]
    [HarmonyPatch("DisplaySituationTokenOnTable")]
    [HarmonyPatch(new Type[] { typeof(SituationToken), typeof(Context)})]
    class TabletopTokenContainer_DisplaySituationTokenOnTable_Patch
    {
        // This runs right after a situation (verb) token is placed onto the table from elsewhere
        static void Postfix(ref SituationToken token, Context context)
        {
            float x = token.RectTransform.position.x;
            float y = token.RectTransform.position.y;
            Utility.Snap(ref x, ref y);
            token.RectTransform.position = new Vector3(x, y, token.RectTransform.position.z);
        }
    }

    [HarmonyPatch(typeof(DraggableToken))]
    [HarmonyPatch("MoveObject")]
    [HarmonyPatch(new Type[] { typeof(PointerEventData) })]
    class DraggableToken_MoveObject_Patch
    {
        // This runs as a token is being dragged
        static void Postfix(DraggableToken __instance, PointerEventData eventData)
        {
            // Check that we're over the tabletop background. If we're over another element, it looks better to not snap.
            if (eventData.pointerCurrentRaycast.gameObject.GetComponent<TabletopBackground>() == null)
                return;

            float x = __instance.RectTransform.position.x;
            float y = __instance.RectTransform.position.y;
            Utility.Snap(ref x, ref y);
            __instance.RectTransform.position = new Vector3(x, y, __instance.RectTransform.position.z);
        }
    }

    [HarmonyPatch(typeof(DraggableToken))]
    [HarmonyPatch("DelayedEndDrag")]
    [HarmonyPatch(new Type[] { })]
    class DraggableToken_DelayedEndDrag_Patch
    {
        // This runs after a token is dropped (on the table or anywhere else)
        static void Postfix(DraggableToken __instance)
        {
            // Check that we're over the tabletop background. If we're over another element, it looks better to not snap.
            if (!(__instance.TokenContainer is TabletopTokenContainer))
                return;

            float x = __instance.RectTransform.position.x;
            float y = __instance.RectTransform.position.y;
            Utility.Snap(ref x, ref y);
            __instance.RectTransform.position = new Vector3(x, y, __instance.RectTransform.position.z);
        }
    }

    [HarmonyPatch(typeof(Choreographer))]
    [HarmonyPatch("GetTestPoints")]
    [HarmonyPatch(new Type[] { typeof(Vector3), typeof(int), typeof(int) })]
    class Choreographer_GetTestPoints_Patch
    {
        // This is used to find a free location for a card that needs to be moved because of overlap. The original
        // implementation is fixed to an inconvenient step size, and has a bug that makes it never consider vertical
        // moves.
        static bool Prefix(ref Vector2[] __result, Vector3 pos, int startIteration, int maxIteration)
        {
            List<Vector2> vector2Array = new List<Vector2>();
            int index1 = 0;
            for (int index2 = 1; index2 < 2 + maxIteration * 2; ++index2)
            {
                float num1 = index2 % 2 != 0 ? (float)(index2 / 2) : (float)-(index2 / 2);
                for (int index3 = 1; index3 < 2 + maxIteration * 2; ++index3)
                {
                    if (index3 > startIteration * 2 - 1 || index2 > startIteration * 2 - 1)
                    {
                        float num2 = index3 % 2 != 0 ? (float)-(index3 / 2) : (float)(index3 / 2);
                        vector2Array.Add(new Vector2(pos.x + num2 * Utility.xStep, pos.y + num1 * Utility.yStep));
                        ++index1;
                    }
                }
            }
            __result = vector2Array.ToArray();

            // Don't run original function
            return false;
        }
    }
}
