// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable CheckNamespace

namespace Dimensionality;

/// <summary>
/// Describes the state of the pocket dimension.
/// </summary>
public enum DimensionState
{
    /// <summary>
    /// The dimension is shown over the active world. Inputs are passed to the dimension instead of the active world.
    /// </summary>
    OverlayActive,
    /// <summary>
    /// The dimension is shown over the active world. Inputs are passed to the active world instead of the dimension.
    /// </summary>
    OverlayInactive,
    /// <summary>
    /// The dimension is entirely hidden
    /// </summary>
    Hidden
}