using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SpecZones.Systems;

[Serializable, NetSerializable]
public sealed partial class SpecZoneKeyDoAfterEvent : SimpleDoAfterEvent;
