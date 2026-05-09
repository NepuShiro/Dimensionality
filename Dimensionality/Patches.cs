using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonoMod.ModInterop;
using Renderite.Shared;

namespace Dimensionality;

/// <summary>
/// 
/// </summary>
[HarmonyPatch]
public static class Patches
{
    [HarmonyPatch(typeof(RenderManager), "ComputeRenderUpdate"), HarmonyPostfix]
    public static void RenderManagerComputeRenderUpdate(RenderManager __instance) => DimensionManager.UpdateWorld(__instance.World, __instance._stagedRenderSpaceUpdate);

    //! Moved from FrooxEngineRunner.UpdateHeadOutput() -> RenderManager.ComputeRenderUpdate()
    //
    // [HarmonyPatch(typeof(FrooxEngineRunner), "UpdateHeadOutput"), HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> FrooxEngineRunnerUpdateHeadOutput(IEnumerable<CodeInstruction> instructions) { }

    [HarmonyPatch(typeof(ScreenController), "OnAttach"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScreenControllerOnAttach(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);

    [HarmonyPatch(typeof(World), "RunWorldEvents"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> WorldRunWorldEvents(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);

    [HarmonyPatch(typeof(ScreenController), "OnInputUpdate"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScreenControllerOnInputUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);

    [HarmonyPatch(typeof(InteractionLaser), "UpdateLaser"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractionLaserUpdateLaser(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions, 0);

    [HarmonyPatch(typeof(InteractionHandler), "OnInputUpdate"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractionHandlerOnInputUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);

    [HarmonyPatch(typeof(InteractionHandler), "OpenContextMenu"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractionHandlerOpenContextMenu(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);

    [HarmonyPatch(typeof(InteractionHandler), "TryOpenContextMenu"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractionHandlerTryOpenContextMenu(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);

    [HarmonyPatch(typeof(InteractionHandler), "UpdateUserspaceToolOffsets"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractionHandlerUpdateUserspaceToolOffsets(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);

    // [HarmonyPatch(typeof(PointerInteractionController), "OnAttach"), HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerOnAttach(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);
    //
    // [HarmonyPatch(typeof(PointerInteractionController), "UpdatePointer"), HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerUpdatePointer(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);
    //
    // [HarmonyPatch(typeof(PointerInteractionController), "GetTouchable"), HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerGetTouchable(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);
    //
    // [HarmonyPatch(typeof(PointerInteractionController), "BeforeInputUpdate"), HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerBeforeInputUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);
    //
    // [HarmonyPatch(typeof(OverlayLayer), "UserspaceOnly", MethodType.Getter), HarmonyTranspiler]
    // private static bool OverlayLayerUserspaceOnly(ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }

    [HarmonyPatch]
    private static class PatchBeforeHandUpdate
    {
        private static MethodInfo TargetMethod() => typeof(InteractionHandler).GetInterfaceMap(typeof(IHandTargetInfoSource)).TargetMethods.First(m => m.Name.Contains("BeforeHandUpdate"));

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> InteractionHandlerBeforeHandUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.IsUserspaceLaserActive), MethodType.Getter), HarmonyPostfix]
    public static void InteractionHandlerIsUserspaceLaserActive(InteractionHandler __instance, ref bool __result)
    {
        if (__instance.Side.Value.IsDimensionLaserActive())
        {
            __result |= !__instance.World.IsDimension();
        }
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.HasUserspaceLaserHitTarget), MethodType.Getter), HarmonyPostfix]
    public static void InteractionHandlerHasUserspaceLaserHitTarget(InteractionHandler __instance, ref bool __result)
    {
        if (__instance.Side.Value.HasDimensionLaserHitTarget())
        {
            __result |= !__instance.World.IsDimension();
        }
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.IsUserspaceHoldingObjects), MethodType.Getter), HarmonyPostfix]
    public static void InteractionHandlerIsUserspaceHoldingObjects(InteractionHandler __instance, ref bool __result)
    {
        if (__instance.Side.Value.IsDimensionHoldingObjects())
        {
            __result |= !__instance.World.IsDimension();
        }
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.MaxLaserDistance), MethodType.Getter), HarmonyPostfix]
    public static void InteractionHandlerMaxLaserDistance(InteractionHandler __instance, ref float __result)
    {
        if (__instance.World.IsDimension())
        {
            __result = Userspace.GetControllerData(__instance.Side.Value).distance;
        }
    }

    //     private const string CommentPrefix = "PocketDimension.";
    //
    //     // Use RunPressed instead of OnAwake and Subscribing to LocalPressed.
    //     [HarmonyPatch(typeof(Button), "RunPressed"), HarmonyPostfix]
    //     public static void ButtonOnAwakePostfix(Button __instance)
    //     {
    // #if !DEBUG
    //             if (!__instance.World.IsUserspace()) return;
    // #endif
    //
    //         Comment buttonComment = __instance?.Slot?.GetComponent<Comment>();
    //         if (buttonComment == null)
    //         {
    //             return;
    //         }
    //
    //         if (string.IsNullOrEmpty(buttonComment.Text.Value) || !buttonComment.Text.Value.StartsWith(CommentPrefix))
    //         {
    //             return;
    //         }
    //
    //         string methodName = buttonComment.Text.Value.Substring(CommentPrefix.Length);
    //
    //         switch (methodName)
    //         {
    //             case "Start":
    //                 Task.Run(DimensionManager.StartDimensionAsync);
    //                 break;
    //             case "Stop":
    //                 Task.Run(DimensionManager.StopDimensionAsync);
    //                 break;
    //             default:
    //                 Plugin.Log.LogWarning("Unknown dimension button method " + methodName);
    //                 break;
    //         }
    //     }

    private static class ILHelper
    {
        private static readonly MethodInfo IsUserspace = AccessTools.Method(typeof(WorldExtensions), nameof(WorldExtensions.IsUserspace));
        private static readonly MethodInfo GetUserspaceWorld = AccessTools.PropertyGetter(typeof(Userspace), nameof(Userspace.UserspaceWorld));
        private static readonly MethodInfo ShouldSkipMethod = AccessTools.Method(typeof(ILHelper), nameof(IsUserspaceOrDimension));
        private static readonly MethodInfo GetUserspacePatchMethod = AccessTools.Method(typeof(ILHelper), nameof(GetUserspacePatch));

        public static IEnumerable<CodeInstruction> ShouldSkipTranspiler(IEnumerable<CodeInstruction> instructions, int idx = -1, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            int count = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(IsUserspace))
                {
                    if (idx == -1 || idx == count)
                    {
                        yield return new CodeInstruction(OpCodes.Call, ShouldSkipMethod);
                    }
                    else
                    {
                        yield return instruction;
                    }
                    count++;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (count == 0)
            {
                Plugin.Log.LogWarning($"ShouldSkipTranspiler could not patch | {caller}({line})");
            }
            else
            {
                Plugin.Log.LogInfo($"ShouldSkipTranspiler patched amount: {count} | {caller}({line})");
            }
        }

        public static bool IsUserspaceOrDimension(World world) => world.IsUserspace() || world.IsDimension();

        public static World? GetUserspacePatch(InteractionHandler h) => IsUserspaceOrDimension(h.World) ? h.World : null;

        public static IEnumerable<CodeInstruction> EqualityAlsoCheckForDimensionTranspiler(IEnumerable<CodeInstruction> instructions, int idx = -1, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            int count = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(GetUserspaceWorld))
                {
                    if (idx == -1 || idx == count)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, GetUserspacePatchMethod);
                    }
                    else
                    {
                        yield return instruction;
                    }
                    count++;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (count == 0)
            {
                Plugin.Log.LogWarning($"EqualityAlsoCheckForDimensionTranspiler could not patch | {caller}({line})");
            }
            else
            {
                Plugin.Log.LogInfo($"EqualityAlsoCheckForDimensionTranspiler patched amount: {count} | {caller}({line})");
            }
        }
    }
}