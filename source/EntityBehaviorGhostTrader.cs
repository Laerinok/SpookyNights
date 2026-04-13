using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using System;

namespace SpookyNights
{
  public class EntityBehaviorGhostTrader : EntityBehavior
  {
    private ServerConfig? _config;
    private long _tickListenerId;

    public EntityBehaviorGhostTrader(Entity entity) : base(entity)
    {
    }

    public override string PropertyName() => "ghosttrader";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
      base.Initialize(properties, attributes);

      if (entity.Api.Side == EnumAppSide.Server)
      {
        _config = ConfigManager.ServerConf;
        _tickListenerId = entity.Api.Event.RegisterGameTickListener(CheckDaytime, 5000);
      }
    }

    private void CheckDaytime(float dt)
    {
      if (!entity.Alive || _config == null || !_config.UseTimeBasedSpawning || !_config.SpawnOnlyAtNight) return;

      if (!IsNightTime(entity.Api, _config))
      {
        DamageSource despawnSource = new DamageSource()
        {
          Source = EnumDamageSource.Internal,
          Type = EnumDamageType.Heal
        };

        entity.Die(EnumDespawnReason.Death, despawnSource);
      }
    }

    private bool IsNightTime(ICoreAPI api, ServerConfig config)
    {
      if ("Auto".Equals(config.NightTimeMode, StringComparison.OrdinalIgnoreCase))
      {
        return api.World.Calendar.GetDayLightStrength(entity.Pos.X, entity.Pos.Z) < 0.85f;
      }
      else
      {
        float hourOfDay = api.World.Calendar.HourOfDay;
        float nightStart = config.NightStartHour;
        float nightEnd = config.NightEndHour;

        if (nightStart > nightEnd)
          return hourOfDay >= nightStart || hourOfDay < nightEnd;

        return hourOfDay >= nightStart && hourOfDay < nightEnd;
      }
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      base.OnEntityDespawn(despawn);
      if (entity.Api.Side == EnumAppSide.Server)
      {
        entity.Api.Event.UnregisterGameTickListener(_tickListenerId);
      }
    }
  }
}