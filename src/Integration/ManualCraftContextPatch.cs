using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow.Elements;
using HarmonyLib;

namespace AudioManager
{
    [HarmonyPatch]
    internal static class ManualCraftContextPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var type in new[] { typeof(RecipeElement), typeof(ResourceElement), typeof(Forest), typeof(Inn), typeof(TrainingGrounds), typeof(Disenchanter) })
                yield return AccessTools.DeclaredMethod(type, "DoManualCraft");
        }

        static void Prefix(out PlaybackScope __state) => __state = new PlaybackScope(AudioChannel.Crafting);
        static Exception Finalizer(Exception __exception, PlaybackScope __state) { __state.Dispose(); return __exception; }
    }
}
