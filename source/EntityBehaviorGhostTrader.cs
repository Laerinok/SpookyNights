using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System;

namespace SpookyNights
{
  public class EntityBehaviorGhostTrader : EntityBehavior
  {
    private ServerConfig? _config;
    private long _tickListenerId;

    public EntityBehaviorGhostTrader(Entity entity) : base(entity) { }

    public override string PropertyName() => "ghosttrader";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
      base.Initialize(properties, attributes);

      if (entity.Api.Side == EnumAppSide.Server)
      {
        _config = ConfigManager.ServerConf;

        // On synchronise l'état au chargement (pour masquer le cadavre si on arrive de jour)
        SyncGhostStatus();

        _tickListenerId = entity.Api.Event.RegisterGameTickListener(_ => SyncGhostStatus(), 5000);
      }
    }

    private void SyncGhostStatus()
    {
      if (_config == null || !_config.UseTimeBasedSpawning || !_config.SpawnOnlyAtNight) return;

      bool isNight = IsNightTime(entity.Api, _config);

      if (entity.Alive)
      {
        if (!isNight)
        {
          // LE SOLEIL SE LÈVE : Disparition
          SpawnGhostParticles(false);

          // On le cache avant de le tuer
          SetGhostVisibility(false);

          DamageSource despawnSource = new DamageSource()
          {
            Source = EnumDamageSource.Internal,
            Type = EnumDamageType.Heal
          };
          entity.Die(EnumDespawnReason.Death, despawnSource);

          // On le descend sous terre pour être sûr (CS0618 corrigé en utilisant Pos)
          entity.Pos.Y -= 3.0f;
        }
        else
        {
          // C'EST LA NUIT : Apparition
          // On ne déclenche la fumée que s'il était caché (renderScale à 0)
          if (entity.Attributes.GetFloat("renderScale") < 0.1f)
          {
            SpawnGhostParticles(true);
            SetGhostVisibility(true);
          }
        }
      }
      else
      {
        // CADAVRE : Toujours invisible
        SetGhostVisibility(false);
      }
    }

    private void SetGhostVisibility(bool visible)
    {
      float scale = visible ? 1.0f : 0.0f;
      // On utilise WatchedAttributes pour que le client reçoive l'info immédiatement
      entity.Attributes.SetFloat("renderScale", scale);
      entity.WatchedAttributes.MarkPathDirty("renderScale");
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (damageSource.Type == EnumDamageType.Heal) return;

      if (damageSource.SourceEntity is EntityPlayer player)
      {
        damage = 0; // Immortel

        // Riposte Fatale
        player.ReceiveDamage(new DamageSource()
        {
          Source = EnumDamageSource.Internal,
          Type = EnumDamageType.PiercingAttack,
          SourceEntity = entity
        }, 100f);

        entity.Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/ghost-whisper"), entity);
      }
      base.OnEntityReceiveDamage(damageSource, ref damage);
    }

    private void SpawnGhostParticles(bool appearing)
    {
      int color = appearing ? ColorUtil.ToRgba(150, 100, 100, 120) : ColorUtil.ToRgba(180, 20, 20, 20);
      SimpleParticleProperties smoke = new SimpleParticleProperties()
      {
        MinPos = entity.Pos.XYZ.AddCopy(-0.2, 0.2, -0.2),
        AddPos = new Vec3d(0.4, 1.5, 0.4),
        MinQuantity = 40,
        Color = color,
        GravityEffect = -0.02f,
        LifeLength = 1.5f,
        ParticleModel = EnumParticleModel.Quad,
        MinVelocity = new Vec3f(-0.1f, 0.1f, -0.1f)
      };
      entity.Api.World.SpawnParticles(smoke);
    }

    private bool IsNightTime(ICoreAPI api, ServerConfig config)
    {
      return api.World.Calendar.GetDayLightStrength(entity.Pos.X, entity.Pos.Z) < 0.85f;
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      if (entity.Api.Side == EnumAppSide.Server)
        entity.Api.Event.UnregisterGameTickListener(_tickListenerId);
    }
  }
}