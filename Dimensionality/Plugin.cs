using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using Elements.Core;
#pragma warning disable CS8974 // Converting method group to non-delegate type

// using BepisResoniteWrapper;

namespace Dimensionality;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log = null!;

    public override void Load()
    {
        Log = base.Log;
        
        Config.Bind("General", "Start", default(dummy), new ConfigDescription("Start", null, Start));
        Config.Bind("General", "Stop", default(dummy), new ConfigDescription("Stop", null, Stop));

        // ResoniteHooks.OnEngineReady += () =>
        // {
        //
        // };

        HarmonyInstance.PatchAll();

        Log.LogInfo($"Plugin {PluginMetadata.GUID} is loaded!");
    }

    public static void LogFunny(object message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
    {
        Log.LogInfo($"{message} | {caller}({line})");
    }

    private static void Start() => Task.Run(DimensionManager.StartDimensionAsync);
    private static void Stop() => Task.Run(DimensionManager.StopDimensionAsync);

    public override bool Unload()
    {
        HarmonyInstance.UnpatchSelf();
        return true;
    }
}