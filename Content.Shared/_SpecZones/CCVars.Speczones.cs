using Robust.Shared.Configuration;


namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Should the Speczone maps start paused, and unpause when entered?
    ///     If changed to false during a round, will unpause all paused speczone maps.
    /// </summary>
    public static readonly CVarDef<bool> SpeczonesStartPaused =
        CVarDef.Create("speczones.start_paused", true, CVar.SERVERONLY);
}
