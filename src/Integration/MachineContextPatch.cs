using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using HarmonyLib;

namespace AudioManager
{
    [HarmonyPatch]
    internal static class MachineContextPatch
    {
        static readonly AccessTools.FieldRef<RecipeElement, bool> HandCraft = AccessTools.FieldRefAccess<RecipeElement, bool>("_handCraft");
        static readonly AccessTools.FieldRef<ResourceElement, int> ResourceManual = AccessTools.FieldRefAccess<ResourceElement, int>("_manualQueued");
        static readonly AccessTools.FieldRef<Forest, int> ForestManual = AccessTools.FieldRefAccess<Forest, int>("_manualQueued");

        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var type in new[] { typeof(RecipeElement), typeof(ResourceElement), typeof(Forest), typeof(Refiner), typeof(Disenchanter), typeof(Inn), typeof(TrainingGrounds) })
                yield return AccessTools.DeclaredMethod(type, "Update", new[] { typeof(float) });
        }

        static void Prefix(FlowElement __instance, out PlaybackScope __state)
        {
            // Capture queue provenance before the native completion decrements its counters.
            bool manual = (__instance is RecipeElement recipe && HandCraft(recipe)) ||
                (__instance is ResourceElement resource && ResourceManual(resource) > 0) ||
                (__instance is Forest forest && ForestManual(forest) > 0);
            __state = new PlaybackScope(manual ? AudioChannel.Crafting : AudioChannel.Machines);
        }

        static Exception Finalizer(Exception __exception, PlaybackScope __state) { __state.Dispose(); return __exception; }
    }
}
