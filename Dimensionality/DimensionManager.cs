using Elements.Core;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using Renderite.Shared;
using SkyFrost.Base;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable CheckNamespace

namespace Dimensionality;

/// <summary>
/// Management utility for a pocket dimension.
/// </summary>
public static class DimensionManager
{
    public static World? World { get; private set; }
    private static InteractionHandler? _leftHandler;
    private static InteractionHandler? _rightHandler;
    private static ITool? _leftUserTool;
    private static ITool? _rightUserTool;

    private static CancellationTokenSource? _cancellationTokenSource;

    private static void SetupWorld(World world)
    {
        CommonAvatarBuilder builder = world.AddSlot("Avatar Builder").AttachComponent<CommonAvatarBuilder>();
        builder.SetupServerVoice.Value = false;
        builder.SetupClientVoice.Value = false;
        builder.AllowLocomotion.Value = false;
        builder.FillEmptySlots.Value = false;
        builder.SetupLocomotion.Value = false;

        // builder.SetupNameBadges.Value = false;
        // builder.SetupIconBadges.Value = false;

        builder.SetupItemShelves.Value = Engine.Current.InputInterface.HeadOutputDevice.IsVR();

        Slot userSlot = world.AddSlot("UserRoot", false);
        UserRoot userRoot = userSlot.AttachComponent<UserRoot>();
        world.LocalUser.Root = userRoot;

        builder.BuildDevices(world.LocalUser, userRoot, userSlot, out Slot _, out List<InteractionHandler> interactions);
        foreach (InteractionHandler handler in interactions)
        {
            UserspacePointer pointer = world.AddSlot($"{handler.Side} {nameof(UserspacePointer)}", false).AttachComponent<UserspacePointer>();
            handler.Equip(pointer, true);
            pointer.Slot.SetIdentityTransform();
            handler.EquippingEnabled.Value = false;
            handler.UserScalingEnabled.Value = false;
            handler.VisualEnabled.Value = false;

            switch ((Chirality)handler.Side)
            {
                case Chirality.Left:
                    _leftHandler = handler;
                    _leftUserTool = pointer;
                    break;
                case Chirality.Right:
                    _rightHandler = handler;
                    _rightUserTool = pointer;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        AvatarManager manager = userSlot.AttachComponent<AvatarManager>();
        manager.AutoAddNameBadge.Value = false;
        manager.AutoAddIconBadge.Value = false;
        manager.AutoAddLiveIndicator.Value = false;
        manager.EmptySlotHandler.Target = Userspace.Current;
        manager.FillEmptySlots();
    }

    /// <summary>
    /// Starts the pocket dimension.
    /// </summary>
    /// <remarks>Does nothing if the world is already running.</remarks>
    public static async Task StartDimensionAsync()
    {
        if (World != null && !World.IsDisposed)
            return;

        World world = await Userspace.OpenWorld(new WorldStartSettings
        {
            InitWorld = SetupWorld,
            AutoFocus = false,
            HideFromListing = true,
            GetExisting = true,
            DefaultAccessLevel = SessionAccessLevel.Private,
            FetchedWorldName = "Pocket Dimension",
            CreateLoadIndicator = false,
        });

        // world.Focus = World.WorldFocus.Focused;
        world.Name = "Pocket Dimension";

        _ = Task.Run(() => world.Coroutines.StartTask(async w =>
        {
            await new NextUpdate();
            w.RunSynchronously(() =>
            {
                Slot? inspector = w.RootSlot.OpenInspectorForTarget();
                if (inspector == null)
                    return;

                inspector.PositionInFrontOfUser(float3.Backward, float3.Forward * 2);
            });

            // w.RunSynchronously(() =>
            // {
            //     UpdatePosition(world);
            // });
        }, world));

        Engine.Current.WorldManager.OverlayWorld(world);

        _cancellationTokenSource = new CancellationTokenSource();
        // _ = world.Coroutines.StartTask(async w =>
        // {
        //     while (!_cancellationTokenSource.IsCancellationRequested)
        //     {
        //         await new NextUpdate();
        //
        //         UpdatePosition(w);
        //     }
        // }, world);

        World = world;
    }

    // Copy the focused world's Transform to the Dimension
    // Copy the User's Position and scale to the Dimension
    internal static void UpdateWorld(World world, RenderSpaceUpdate spaceUpdate)
    {
        if (DimensionManager.World == null || DimensionManager.World == world) return;

        if (spaceUpdate.isActive)
        {
            World focusedWorld = Engine.Current.WorldManager.FocusedWorld;

            // UnityEngine.Transform transform1 = ((WorldConnector)focusedWorld.Connector).WorldRoot.transform;
            // UnityEngine.Transform transform2 = ((WorldConnector)world.Connector).WorldRoot.transform;
            //
            // transform2.position = transform1.position;
            // transform2.rotation = transform1.rotation;
            // transform2.localScale = transform1.localScale;

            spaceUpdate.rootTransform = focusedWorld.LocalUserTransform;

            if (world.CanCurrentThreadModify)
            {
                UserRoot focusedUserRoot = focusedWorld.LocalUser.Root;
                UserRoot worldUserRoot = world.LocalUser.Root;

                worldUserRoot.Slot.GlobalPosition = focusedUserRoot.Slot.GlobalPosition;
                worldUserRoot.Slot.GlobalScale = focusedUserRoot.Slot.GlobalScale;

                if (_leftHandler != null && _leftUserTool != null && _leftHandler.ActiveTool == null)
                {
                    _leftHandler.Equip(_leftUserTool, true);
                }

                if (_rightHandler != null && _rightUserTool != null && _rightHandler.ActiveTool == null)
                {
                    _rightHandler.Equip(_rightUserTool, true);
                }
            }
        }
    }

    /// <summary>
    /// Stops the pocket dimension.
    /// </summary>
    /// <remarks>Does nothing if the world is already stopped.</remarks>
    public static async Task StopDimensionAsync()
    {
        if (World == null)
            return;

        if (_cancellationTokenSource != null)
            await _cancellationTokenSource.CancelAsync();

        if (World.IsDisposed)
        {
            ClearWorldReferences();
            return;
        }

        await Userspace.ExitWorld(World);
        ClearWorldReferences();
    }

    private static void ClearWorldReferences()
    {
        World = null;
        _leftHandler = null;
        _rightHandler = null;
        _cancellationTokenSource?.Dispose();
    }
    
    extension(World w)
    {
        public bool IsDimension()
        {
            if (World == null)
                return false;

            return World == w;
        }
        public bool IsNotDimension()
        {
            if (World == null)
                return true;

            return World != w;
        }
    }

    extension(Chirality side)
    {
        private InteractionHandler? Handler()
        {
            return side switch
            {
                Chirality.Left  => _leftHandler,
                Chirality.Right => _rightHandler,
                _               => throw new ArgumentOutOfRangeException(nameof(side), side, null)
            };
        }
        public bool IsDimensionLaserActive()
        {
            if (World == null)
                return false;

            return side.Handler()?.Laser.LaserActive ?? false;
        }
        public bool HasDimensionLaserHitTarget()
        {
            if (World == null)
                return false;

            return side.Handler()?.Laser.CurrentHit != null;
        }
        public bool IsDimensionHoldingObjects()
        {
            if (World == null)
                return false;

            return side.Handler()?.IsHoldingObjects ?? false;
        }
    }
}