using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.SpecZones;

/// <summary>
/// Component for signal propagators.
/// </summary>
[RegisterComponent, Serializable]
public sealed partial class SignalPropagatorComponent : Component
{
    /// <summary>
    /// Is this propagator up-to-date?
    /// </summary>
    public bool FullyUpdated = false;
    public float UpdateAccumulator = 0f;


    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Status = true;


    [DataField]
    public ProtoId<SinkPortPrototype> EnablePort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> DisablePort = "Off";

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public List<ProtoId<SourcePortPrototype>> HighSourcePorts = new() { "On" };

    [DataField]
    public List<ProtoId<SourcePortPrototype>> LowSourcePorts = new() { "Off" };
}
