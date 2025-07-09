using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Content.Shared.SpecZones.Systems;
using Content.Shared.SpecZones.Types;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Microsoft.CodeAnalysis;
using Robust.Shared.Random;
using Content.Shared._Goobstation.Wizard.Traps;
using Robust.Shared.Map;
using Content.Server.Spawners.Components;
using Content.Shared.Mind;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Stunnable;
using System.Threading.Tasks;
using Content.Shared.Popups;
using Content.Server.IdentityManagement;
using Robust.Server.Audio;
using Content.Server.Administration.Systems;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.RCD.Components;
using Content.Shared.Tag;
using Content.Shared.Wires;
using Content.Shared.Doors.Components;
using Content.Server.Atmos.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Server.Player;
using Content.Shared.CCVar;
using System.Runtime.CompilerServices;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;
using Content.Shared.Maps;

namespace Content.Server.SpecZones;

public sealed class SpecZoneSystem : SharedSpecZoneSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TurfSystem _turfSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoaderSystem = default!;
    [Dependency] private readonly BiomeSystem _biomes = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SparksSystem _sparks = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenateSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    // Why the fuck would you even turn this off
    private bool _specZonesStartPaused = true;

    private List<EntityCoordinates> _zoneExitPositions = new();

    private static DeserializationOptions _zoneDeserializationOptions = new DeserializationOptions { InitializeMaps = true };


    private EntityQuery<MetaDataComponent> _metaDataQuery;
    private EntityQuery<SpecialZoneMapComponent> _specZoneQuery;


    public override void Initialize()
    {
        base.Initialize();

        _metaDataQuery = GetEntityQuery<MetaDataComponent>();
        _specZoneQuery = GetEntityQuery<SpecialZoneMapComponent>();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<EndSpecialZoneOnTriggerComponent, TriggerEvent>(OnEndZoneTrigger);

        SubscribeLocalEvent<SpecZoneKeyComponent, UseInHandEvent>(OnKeyUseInhand);
        SubscribeLocalEvent<SpecZoneKeyComponent, SpecZoneKeyDoAfterEvent>(OnBadDecision);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);

        SubscribeLocalEvent<IsStaticByMap>(IsEntityStatic);

        _zoneDeserializationOptions.PauseMaps = _specZonesStartPaused;
        Subs.CVar(_configManager, CCVars.SpeczonesStartPaused, ZonePausedCvarChanged, true);
    }

    private void IsEntityStatic(ref IsStaticByMap args)
    {
        var transform = Transform(args.Target);
        var mapUid = transform.MapUid;

        if (mapUid != null)
            args.Static |= _specZoneQuery.HasComponent(mapUid.Value);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent args)
    {
        _zoneExitPositions.Clear();
    }

    public List<Entity<SpecialZoneMapComponent>> GetZoneMapList()
    {
        var zoneEnum = EntityManager.AllEntityQueryEnumerator<SpecialZoneMapComponent>();

        List<Entity<SpecialZoneMapComponent>> zones = new();
        while (zoneEnum.MoveNext(out var uid, out var zoneMapComponent))
            zones.Add((uid, zoneMapComponent));

        return zones;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<Entity<SpecialZoneMapComponent>> GetZoneMapList(out int count)
    {
        var mapList = GetZoneMapList();

        count = mapList.Count();
        return mapList;
    }

    /// <returns>A dictionary, with the Key being the zone's ID, and Value being the zone.</returns>
    public Dictionary<string, Entity<SpecialZoneMapComponent>> GetZoneMapDictionary()
    {
        var zoneEnum = EntityManager.AllEntityQueryEnumerator<SpecialZoneMapComponent>();

        Dictionary<string, Entity<SpecialZoneMapComponent>> zones = new();
        while (zoneEnum.MoveNext(out var uid, out var zoneMapComponent))
            zones[zoneMapComponent.ZoneId] = (uid, zoneMapComponent);

        return zones;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<string, Entity<SpecialZoneMapComponent>> GetZoneMapDictionary(out int count)
    {
        var mapDict = GetZoneMapDictionary();

        count = mapDict.Count();
        return mapDict;
    }



    private void ZonePausedCvarChanged(bool isStartPaused)
    {
        if (_specZonesStartPaused == isStartPaused)
            return;

        if (isStartPaused == true)
            return;

        var zoneEnum = EntityManager.AllEntityQueryEnumerator<SpecialZoneMapComponent>();
        while (zoneEnum.MoveNext(out var uid, out var zoneMapComponent))
            _mapSystem.SetPaused(uid, false);
    }


    /// <summary>
    /// Unpauses the zone and sets it as awake. Should be called when the zone starts being in use/stops being in use.
    /// Does not check if it's already unpaused.
    /// </summary>
    // inlined because this is private and only used in 1 method ever for now
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetZoneAwake(Entity<SpecialZoneMapComponent> zone, bool status)
    {
        var (uid, zoneMapComponent) = zone;

        _mapSystem.SetPaused(uid, !status);
        zoneMapComponent.Awake = status;

        Log.Info($"{(status ? "Waking" : "Sleeping")} special zone {zone.Comp.ZoneId}");
    }

    /// <summary>
    /// Unpause/pause if the zone isn't already awake/sleeping.
    /// Doesn't do do anything when <see cref="CCVars.SpeczonesStartPaused"/> is off.
    /// Should be called when the zone starts being in use/stops being in use.
    /// </summary>
    /// <returns>Whether the zone was set as according to <paramref name="status"/>. Returns false if it was already awake.</returns>
    public bool TrySetZoneAwake(Entity<SpecialZoneMapComponent> zone, bool status)
    {
        if (!_specZonesStartPaused)
            return false;

        if (zone.Comp.Awake == status)
            return false;

        SetZoneAwake(zone, status);

        return true;
    }


    /// <summary>
    /// Does not initialise maps as actual special zones, but only sets it up
    /// (i.e., making things indestructible/unhackable)
    /// </summary>
    // This is rather laggy but it only happens once so ig it's fine.
    private void SetupZoneMaps(List<EntityUid> mapUids)
    {
        var damageableEnumerator = EntityManager.AllEntityQueryEnumerator<DamageableComponent>();
        while (damageableEnumerator.MoveNext(out var entityUid, out var damageable))
        {
            var transformComponent = Transform(entityUid);
            var entityMapUid = transformComponent.MapUid;
            if (entityMapUid == null || !mapUids.Contains(entityMapUid.Value))
                continue;

            var metadata = _metaDataQuery.GetComponent(entityUid);
            if (!ShouldMakeInvincibleAndEdgecase(entityUid, damageable, metadata))
                continue;

            RemComp(entityUid, damageable);

            if (TryComp<RCDDeconstructableComponent>(entityUid, out var rcdDeconComp))
                RemComp(entityUid, rcdDeconComp);

            if (TryComp<AnchorableComponent>(entityUid, out var anchorableComp))
                RemComp(entityUid, anchorableComp); // Why, sloth? Why make it readonly?

            if (TryComp<AccessReaderComponent>(entityUid, out var accessComponent))
                accessComponent.BreakOnAccessBreaker = false;
        }

    }

    private EntityUid InitZonePrototype(SpecialZonePrototype zonePrototype)
    {
        if (!_mapLoaderSystem.TryLoadMap(zonePrototype.MapPath, out var map, out var grids, _zoneDeserializationOptions))
        {
            Log.Error($"Could not load special zone {zonePrototype.ID}");
            return EntityUid.Invalid;
        }

        var mapUid = map.Value.Owner;

        var mapZoneComponent = EnsureComp<SpecialZoneMapComponent>(mapUid);
        mapZoneComponent.ZoneId = zonePrototype.ID;

        if (zonePrototype.ZoneBiome != null && _prototypeManager.TryIndex(zonePrototype.ZoneBiome, out BiomeTemplatePrototype? zoneBiome))
            _biomes.EnsurePlanet(mapUid, zoneBiome);

        return mapUid;
    }

    private bool ShouldMakeInvincibleAndEdgecase(EntityUid entityUid, DamageableComponent damageable, MetaDataComponent metadata)
    {
        if (HasComp<AirtightComponent>(entityUid))
            return true;

        if (metadata.EntityPrototype != null)
        {
            var entityProtoId = metadata.EntityPrototype.ID;

            if (entityProtoId.Contains("wall", StringComparison.OrdinalIgnoreCase))
                return true;

            if (entityProtoId.Contains("grille", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (HasComp<DoorComponent>(entityUid))
        {
            if (TryComp<WiresPanelComponent>(entityUid, out var wiresPanelComponent))
                RemComp(entityUid, wiresPanelComponent);

            return true;
        }

        if (_tagSystem.HasTag(entityUid, "Window"))
            return true;

        return false;
    }

    private void FindExitPositions()
    {
        _zoneExitPositions.Clear();
        var spawnEnum = EntityQueryEnumerator<SpawnPointComponent>();
        var possibleExitPositions = new List<EntityCoordinates>();

        while (spawnEnum.MoveNext(out var spawnUid, out var spawnComponent))
            possibleExitPositions.Add(Transform(spawnUid).Coordinates);

        if (possibleExitPositions.Count == 0)
            return;

        _zoneExitPositions = possibleExitPositions;
    }

    public EntityCoordinates? GetRandomZoneEntrance(string zoneId)
    {
        var zone = GetZoneMapDictionary()[zoneId];

        var entranceEnum = EntityQueryEnumerator<SpecialZoneSpawnComponent>();
        var possibleEntrancePositions = new List<EntityCoordinates>();

        while (entranceEnum.MoveNext(out var spawnUid, out var spawnComponent))
        {
            if (spawnComponent.ZoneId != zoneId)
                continue;

            possibleEntrancePositions.Add(Transform(spawnUid).Coordinates);
        }

        if (possibleEntrancePositions.Count == 0)
            return null;

        return _random.Pick(possibleEntrancePositions);
    }

    public bool EjectFromZone(EntityUid ejecteeUid)
    {
        if (_zoneExitPositions.Count == 0)
            return false;

        var exitPosition = _random.Pick(_zoneExitPositions);

        // fuck them up a bit
        _transform.SetCoordinates(ejecteeUid, exitPosition);

        try
        {
            _stunSystem.TryParalyze(ejecteeUid, ZoneExitEffectDuration, false);
            _popupSystem.PopupCoordinates($"{_identity.GetEntityIdentity(ejecteeUid)} gets twisted back into this realm!", exitPosition, PopupType.MediumCaution);

            // dosparks might randomly die so,,, this is in trycatch
            _sparks.DoSparks(exitPosition);
        }
        catch (Exception ejectEx) { Log.Error($"Exception thrown when trying to eject entity from zone! Uid: {ejecteeUid}, Exception: {ejectEx.Message}, Stack: {ejectEx.StackTrace}"); }

        return true;
    }

    public void InsertIntoZone(EntityUid entityUid, EntityCoordinates position)
    {
        // fuck them up a bit #2
        _transform.SetCoordinates(entityUid, position);
        _sparks.DoSparks(position);

        _rejuvenateSystem.PerformRejuvenate(entityUid);
        _stunSystem.TryParalyze(entityUid, ZoneExitEffectDuration, false);
    }

#pragma warning disable RA0030
    public void EndZone(Entity<SpecialZoneMapComponent> zone, bool shouldSleepZone = true)
    {
        var zoneMapUid = zone.Owner;
        var allLivingMinds = _mindSystem.GetAliveHumans();

        FindExitPositions();
        Parallel.ForEach(allLivingMinds, mindEntity =>
        {
            var humanUid = mindEntity.Comp.OwnedEntity;
            if (humanUid == null)
                return;

            if (!TryComp<TransformComponent>(humanUid, out var humanTransform))
                return;

            var humanMapUid = _transform.GetMap(humanTransform.Coordinates);
            if (humanMapUid == null || humanMapUid != zoneMapUid)
                return;

            EjectFromZone(humanUid.Value);

            if (_player.TryGetSessionByEntity(humanUid.Value, out var mind))
                _audio.PlayGlobal(ZoneFinishSoundSpec, mind);
        });

        if (shouldSleepZone)
            TrySetZoneAwake(zone, false);
    }
#pragma warning restore RA0030

    public Entity<SpecialZoneMapComponent> GetRandomZone() => GetZoneMapList(out var count)[_random.Next(0, count)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetRandomZoneId() => GetZoneMapDictionary(out var count).ElementAt(_random.Next(0, count)).Key;

    private void OnRoundStarting(RoundStartingEvent roundStartEv)
    {
        var zonePrototypes = _prototypeManager.EnumeratePrototypes<SpecialZonePrototype>().ToList();
        var zoneUids = new List<EntityUid>();

        zonePrototypes.ForEach(zonePrototype => zoneUids.Add(InitZonePrototype(zonePrototype)));
        SetupZoneMaps(zoneUids);
    }

    private void OnEndZoneTrigger(Entity<EndSpecialZoneOnTriggerComponent> triggerEnt, ref TriggerEvent triggerEv)
    {
        var zoneMapDictionary = GetZoneMapDictionary();
        var triggerEndingZoneId = triggerEnt.Comp.ZoneId;

        if (triggerEndingZoneId != null)
        {
            if (!zoneMapDictionary.TryGetValue(triggerEndingZoneId, out var activeZone))
                return;

            EndZone(activeZone);

            return;
        }

        var triggerEntMapUid = _transform.GetMap(triggerEnt.Owner);
        if (triggerEntMapUid != null && _specZoneQuery.TryGetComponent(triggerEntMapUid, out var triggerEntMapSpecZoneComponent))
            EndZone((triggerEntMapUid.Value, triggerEntMapSpecZoneComponent));
    }

    private void OnKeyUseInhand(Entity<SpecZoneKeyComponent> key, ref UseInHandEvent args)
    {
        var user = args.User;
        var keyDoAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(5), new SpecZoneKeyDoAfterEvent(), key.Owner)
        {
            DistanceThreshold = 1f,
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(keyDoAfter))
            _popupSystem.PopupEntity($"{_identity.GetEntityIdentity(user)} raises the key into the air...", user, PopupType.Medium);

        FindExitPositions();
    }

    private void OnBadDecision(Entity<SpecZoneKeyComponent> key, ref SpecZoneKeyDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;
        var targetZoneId = key.Comp.ZoneId ?? GetRandomZoneId();

        if (!GetZoneMapDictionary().TryGetValue(targetZoneId, out var targetZone))
            return;

        TrySetZoneAwake(targetZone, true);

        var zoneEntrancePosition = GetRandomZoneEntrance(targetZoneId);
        if (zoneEntrancePosition == null)
            return;

        _transform.TryGetMapOrGridCoordinates(user, out var useCoordinates);

        // you're fucked now
        InsertIntoZone(user, zoneEntrancePosition.Value);
        EjectFromZone(key.Owner);

        if (_mindSystem.TryGetMind(user, out var mindId, out var mindComponent) && _player.TryGetSessionByEntity(mindId, out var mind))
            _audio.PlayGlobal(ZoneEnterSoundSpec, mind);

        if (useCoordinates != null)
            _popupSystem.PopupCoordinates($"{_identity.GetEntityIdentity(user)} disappears in a flash of light!", useCoordinates.Value, PopupType.LargeCaution);
    }
}



/// <summary>
/// Raised on an entity to tell if it's map's state should allow it to be tampered with.
/// </summary>
[ByRefEvent]
public sealed class IsStaticByMap : EntityEventArgs
{
    public bool Static;

    public readonly EntityUid Target;

    public IsStaticByMap(EntityUid target) { Static = false; Target = target; }
}
