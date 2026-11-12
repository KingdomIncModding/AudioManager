using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Source.Flow.Elements;
using HarmonyLib;

namespace AudioManager
{
    [HarmonyPatch]
    internal static class CombatContextPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Arena), "DoManualCraft");
            yield return AccessTools.DeclaredMethod(typeof(Arena), "Update", new[] { typeof(float) });
            yield return AccessTools.Method(typeof(Dungeon), "CheckForParty");
        }

        static void Prefix(out PlaybackScope __state) => __state = new PlaybackScope(AudioChannel.Combat);
        static Exception Finalizer(Exception __exception, PlaybackScope __state) { __state.Dispose(); return __exception; }
    }
}
