using System.Reflection;
using System.Reflection.Emit;
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
    [HarmonyPatch(typeof(RenderManager), "ComputeRenderUpdate")]
    [HarmonyPostfix]
    public static void ChangeStagedRenderUpdate(RenderManager __instance, RenderSpaceUpdate ____stagedRenderSpaceUpdate) => DimensionManager.UpdateWorld(__instance.World, ____stagedRenderSpaceUpdate);

    //! Moved from FrooxEngineRunner.UpdateHeadOutput() -> RenderManager.ComputeRenderUpdate()
    //
    // [HarmonyPatch(typeof(FrooxEngineRunner), "UpdateHeadOutput")]
    // [HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> FrooxEngineRunnerUpdateHeadOutput(IEnumerable<CodeInstruction> instructions) { }

    [HarmonyPatch(typeof(ScreenController), "OnAttach")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScreenControllerOnAttach(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);

    [HarmonyPatch(typeof(World), "RunWorldEvents")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> WorldRunWorldEvents(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);

    [HarmonyPatch(typeof(ScreenController), "OnInputUpdate")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScreenControllerOnInputUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);

    // [HarmonyPatch(typeof(PointerInteractionController), "OnAttach")]
    // [HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerOnAttach(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);
    //
    // [HarmonyPatch(typeof(PointerInteractionController), "UpdatePointer")]
    // [HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerUpdatePointer(IEnumerable<CodeInstruction> instructions) => ILHelper.ShouldSkipTranspiler(instructions);
    //
    //
    // [HarmonyPatch(typeof(OverlayLayer), "UserspaceOnly", MethodType.Getter)]
    // [HarmonyTranspiler]
    // private static bool OverlayLayerUserspaceOnly(ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }
    //
    // [HarmonyPatch(typeof(PointerInteractionController), "GetTouchable")]
    // [HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerGetTouchable(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);
    //
    // [HarmonyPatch(typeof(PointerInteractionController), "BeforeInputUpdate")]
    // [HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> PointerInteractionControllerBeforeInputUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);

    [HarmonyPatch]
    private static class PatchBeforeHandUpdate
    {
        private static MethodInfo TargetMethod()
        {
            return typeof(InteractionHandler).GetInterfaceMap(typeof(IHandTargetInfoSource)).TargetMethods.First(m => m.Name.Contains("BeforeHandUpdate"));
        }

        [HarmonyPrefix]
        private static bool Prefix(InteractionHandler __instance)
        {
            if (!__instance.IsContextMenuOpen && !__instance.IsHoldingObjectsWithLaser && (__instance.World != Userspace.UserspaceWorld || !__instance.World.IsDimension()))
            {
                __instance._beforeHandUpdate ??= __instance.RunBeforeHandUpdate;

                __instance.RunInUpdateScope(__instance._beforeHandUpdate);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.IsUserspaceLaserActive), MethodType.Getter)]
    [HarmonyPostfix]
    public static void InteractionHandlerIsUserspaceLaserActive(InteractionHandler __instance, ref bool __result)
    {
        __result |= __instance.Side.Value.IsDimensionLaserActive() && __instance.World != DimensionManager.World;
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.HasUserspaceLaserHitTarget), MethodType.Getter)]
    [HarmonyPostfix]
    public static void InteractionHandlerHasUserspaceLaserHitTarget(InteractionHandler __instance, ref bool __result)
    {
        __result |= __instance.Side.Value.HasDimensionLaserHitTarget() && __instance.World != DimensionManager.World;
    }
    
    [HarmonyPatch(typeof(InteractionHandler), "OnInputUpdate")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> InteractionHandlerOnInputUpdate(IEnumerable<CodeInstruction> instructions) => ILHelper.EqualityAlsoCheckForDimensionTranspiler(instructions);


    // [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.IsUserspaceHoldingObjects), MethodType.Getter)]
    // [HarmonyPrefix]
    // public static bool InteractionHandlerIsUserspaceHoldingObjects(InteractionHandler __instance, ref bool __result)
    // {
    //     if (__instance.Side.Value.IsDimensionHoldingObjects())
    //     {
    //         __result = !__instance.World.IsDimension();
    //         return false;
    //     }
    //     
    //     return true;
    // }

    //     private const string CommentPrefix = "PocketDimension.";
    //
    //     // Use RunPressed instead of OnAwake and Subscribing to LocalPressed.
    //     [HarmonyPatch(typeof(Button), "RunPressed")]
    //     [HarmonyPostfix]
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
        private static readonly MethodInfo IsDimension = AccessTools.Method(typeof(DimensionManager), nameof(DimensionManager.IsDimension));
        private static readonly MethodInfo GetWorld = AccessTools.PropertyGetter(typeof(Worker), nameof(Worker.World));
        private static readonly MethodInfo GetUserspaceWorld = AccessTools.PropertyGetter(typeof(Userspace), nameof(Userspace.UserspaceWorld));
        private static readonly MethodInfo GetInputInterface = AccessTools.PropertyGetter(typeof(Worker), nameof(Worker.InputInterface));
        private static readonly MethodInfo ShouldSkipMethod = AccessTools.Method(typeof(ILHelper), nameof(IsUserspaceOrDimension));
        private static readonly MethodInfo ShouldSkip2Method = AccessTools.Method(typeof(ILHelper), nameof(IsNeither));
        private static readonly MethodInfo GetUserspacePatchMethod = AccessTools.Method(typeof(ILHelper), nameof(GetUserspacePatch));

        public static IEnumerable<CodeInstruction> ShouldSkipTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(IsUserspace))
                {
                    yield return new CodeInstruction(OpCodes.Call, ShouldSkipMethod);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        public static bool IsUserspaceOrDimension(World world) => world.IsUserspace() || world.IsDimension();
        public static bool IsNeither(World world) => !world.IsUserspace() || !world.IsDimension();

        public static World? GetUserspacePatch(InteractionHandler h) => IsUserspaceOrDimension(h.World) ? h.World : null;

        public static IEnumerable<CodeInstruction> EqualityAlsoCheckForDimensionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(GetUserspaceWorld))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, GetUserspacePatchMethod);
                    found = true;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (!found)
            {
                Plugin.Log.LogWarning("EqualityAlsoCheckForDimensionTranspiler2 death");
            }
        }
    }
}