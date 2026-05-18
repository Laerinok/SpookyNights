using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace SpookyNights
{
  public class EntityBehaviorGhostTrader : EntityBehavior
  {
    private ServerConfig? _config;
    private long _serverTickListenerId;
    private long _clientTickListenerId;

    private Cuboidf? _origHitbox;
    private Cuboidf? _origSelection;

    private int _spectralColorInt;

    public EntityBehaviorGhostTrader(Entity entity) : base(entity) { }

    public override string PropertyName() => "ghosttrader";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
      base.Initialize(properties, attributes);

      _origHitbox = entity.CollisionBox.Clone();
      _origSelection = entity.SelectionBox.Clone();

      // 1. Calculate the exact color based on the fog biomes
      _spectralColorInt = DetermineBiomeFogColor(entity.Code.Path);

      // 2. Apply this color to the merchant's texture (tint)
      entity.WatchedAttributes.SetInt("renderColor", _spectralColorInt);

      if (entity.Api.Side == EnumAppSide.Server)
      {
        _config = ConfigManager.ServerConf;

        if (!entity.WatchedAttributes.HasAttribute("origY"))
        {
          entity.WatchedAttributes.SetDouble("origX", entity.Pos.X);
          entity.WatchedAttributes.SetDouble("origY", entity.Pos.Y);
          entity.WatchedAttributes.SetDouble("origZ", entity.Pos.Z);
        }

        entity.Api.Event.RegisterCallback((dt) => {
          if (entity.Alive) SyncGhostStatus(true);
        }, 2000);

        _serverTickListenerId = entity.Api.Event.RegisterGameTickListener(_ => SyncGhostStatus(false), 5000);
      }
      else
      {
        entity.WatchedAttributes.RegisterModifiedListener("isHidden", () => UpdateClientState());
        UpdateClientState();

        // Start continuous particle emission on the client side (every 100ms)
        _clientTickListenerId = entity.Api.Event.RegisterGameTickListener(SpawnContinuousParticles, 100);
      }
    }

    // Mirrors the logic from SystemCemeteryFog to match the exact fog color
    private int DetermineBiomeFogColor(string path)
    {
      // Default (Plain / Cemetery): 0.65, 0.65, 0.75
      int r = 165, g = 165, b = 191;

      if (path.Contains("temperateforest")) { r = 102; g = 127; b = 102; }
      else if (path.Contains("swamp")) { r = 89; g = 114; b = 102; }
      else if (path.Contains("tropicalforest")) { r = 63; g = 89; b = 38; }
      else if (path.Contains("arid")) { r = 191; g = 153; b = 102; }
      else if (path.Contains("arctic")) { r = 204; g = 229; b = 242; }
      else if (path.Contains("wanderer")) { r = 127; g = 127; b = 127; }

      // Full opacity for the texture render color
      return ColorUtil.ToRgba(255, r, g, b);
    }

    private void UpdateClientState()
    {
      bool isHidden = entity.WatchedAttributes.GetBool("isHidden", false);
      if (isHidden)
      {
        entity.CollisionBox.Set(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        entity.SelectionBox.Set(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
      }
      else
      {
        if (_origHitbox != null) entity.CollisionBox.Set(_origHitbox);
        if (_origSelection != null) entity.SelectionBox.Set(_origSelection);
      }
    }

    private void SyncGhostStatus(bool isInit)
    {
      if (entity.Code.Path.Contains("wanderer")) return;

      if (_config == null || !_config.UseTimeBasedSpawning || !_config.SpawnOnlyAtNight) return;

      bool isNight = IsNightTime(entity.Api, _config);
      bool isHidden = entity.WatchedAttributes.GetBool("isHidden", false);

      if (isNight && isHidden)
      {
        entity.State = EnumEntityState.Active;

        double origX = entity.WatchedAttributes.GetDouble("origX", entity.Pos.X);
        double origY = entity.WatchedAttributes.GetDouble("origY", entity.Pos.Y);
        double origZ = entity.WatchedAttributes.GetDouble("origZ", entity.Pos.Z);

        entity.TeleportTo(new Vec3d(origX, origY, origZ));

        entity.WatchedAttributes.SetBool("isHidden", false);
        entity.WatchedAttributes.MarkPathDirty("isHidden");

        if (!isInit) SpawnTransitionParticles(true);
      }
      else if (!isNight && !isHidden)
      {
        if (!isInit) SpawnTransitionParticles(false);

        double origY = entity.WatchedAttributes.GetDouble("origY", entity.Pos.Y);
        double hiddenY = Math.Max(1.0, origY - 2.0);
        entity.TeleportTo(new Vec3d(entity.Pos.X, hiddenY, entity.Pos.Z));

        entity.Pos.Motion.Set(0.0, 0.0, 0.0);
        entity.WatchedAttributes.SetBool("isHidden", true);
        entity.WatchedAttributes.MarkPathDirty("isHidden");

        entity.State = EnumEntityState.Inactive;
      }
    }

    public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
    {
      if (entity.WatchedAttributes.GetBool("isHidden", false))
      {
        handled = EnumHandling.PreventSubsequent;
        return;
      }
      base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
      if (entity.WatchedAttributes.GetBool("isHidden", false))
      {
        damage = 0.0f;
        return;
      }

      if (damageSource.Type == EnumDamageType.Heal) return;
      if (damageSource.SourceEntity is EntityPlayer entityPlayer)
      {
        IServerPlayer? serverPlayer = entityPlayer.Player as IServerPlayer;
        damage = 0.0f;

        double currentTime = entity.Api.World.Calendar.TotalHours;
        double lastStrikeTime = entity.Attributes.GetDouble("lastStrikeTime", 0.0);
        int strikes = entity.Attributes.GetInt("strikeCount", 0);

        if (currentTime - lastStrikeTime > 24.0) strikes = 0;
        strikes++;
        entity.Attributes.SetInt("strikeCount", strikes);
        entity.Attributes.SetDouble("lastStrikeTime", currentTime);

        if (serverPlayer != null)
        {
          switch (strikes)
          {
            case 1:
              serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"<strong><font color=\"#ffcccc\">{Lang.Get("spookynights:ghosttrader-warning-1")}</font></strong>", EnumChatType.Notification);
              entity.Api.World.PlaySoundAt(new AssetLocation("spookynights:creature/drifter/hurt1"), entity);
              break;
            case 2:
              serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"<strong><font color=\"#ff9999\">{Lang.Get("spookynights:ghosttrader-warning-2")}</font></strong>", EnumChatType.Notification);
              entity.Api.World.PlaySoundAt(new AssetLocation("game:sounds/creature/wolf/growl3"), entity);
              break;
            case 3:
              entity.AnimManager?.StartAnimation("Attack");
              entityPlayer.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.PiercingAttack, SourceEntity = entity }, 100f);
              entity.Api.World.PlaySoundAt(new AssetLocation("spookynights:creature/trader/growl1"), entity);
              entity.Attributes.SetInt("strikeCount", 0);
              break;
          }
        }
      }
      base.OnEntityReceiveDamage(damageSource, ref damage);
    }

    // A large puff of smoke when transitioning (day/night)
    private void SpawnTransitionParticles(bool appearing)
    {
      int alpha = appearing ? 180 : 100;
      int r = (_spectralColorInt >> 16) & 0xFF;
      int g = (_spectralColorInt >> 8) & 0xFF;
      int b = _spectralColorInt & 0xFF;

      int particleColor = ColorUtil.ToRgba(alpha, r, g, b);

      SimpleParticleProperties smoke = new SimpleParticleProperties()
      {
        MinPos = entity.Pos.XYZ.AddCopy(-0.5, 0.0, -0.5),
        AddPos = new Vec3d(1.0, 2.0, 1.0),
        MinQuantity = 80,
        AddQuantity = 40,
        Color = particleColor,
        GravityEffect = -0.02f,
        LifeLength = 2.0f,
        ParticleModel = EnumParticleModel.Quad,
        MinSize = 0.5f,
        MaxSize = 1.5f,
        MinVelocity = new Vec3f(-0.1f, 0.1f, -0.1f),
        AddVelocity = new Vec3f(0.2f, 0.2f, 0.2f)
      };

      // Fix: Using EvolvingNatFloat with LINEAR transform to decrease opacity by 1.0 over its lifetime
      smoke.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1.0f);
      entity.Api.World.SpawnParticles(smoke);
    }

    // Continuous particles emitted while the trader is active
    private void SpawnContinuousParticles(float dt)
    {
      if (entity.WatchedAttributes.GetBool("isHidden", false) || !entity.Alive) return;

      int r = (_spectralColorInt >> 16) & 0xFF;
      int g = (_spectralColorInt >> 8) & 0xFF;
      int b = _spectralColorInt & 0xFF;
      int fogColor = ColorUtil.ToRgba(60, r, g, b);

      SimpleParticleProperties ambientMist = new SimpleParticleProperties()
      {
        MinPos = entity.Pos.XYZ.AddCopy(-0.3, 0.1, -0.3),
        AddPos = new Vec3d(0.6, 1.5, 0.6),
        MinQuantity = 1,
        AddQuantity = 1,
        Color = fogColor,
        GravityEffect = -0.01f,
        LifeLength = 1.5f,
        ParticleModel = EnumParticleModel.Quad,
        MinSize = 0.4f,
        MaxSize = 0.8f,
        MinVelocity = new Vec3f(-0.05f, 0.02f, -0.05f),
        AddVelocity = new Vec3f(0.1f, 0.04f, 0.1f)
      };

      // Fix: Using EvolvingNatFloat with LINEAR transform
      ambientMist.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1.0f);
      entity.Api.World.SpawnParticles(ambientMist);
    }

    private bool IsNightTime(ICoreAPI api, ServerConfig config) => api.World.Calendar.GetDayLightStrength(entity.Pos.X, entity.Pos.Z) < 0.85f;

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      if (entity.Api.Side == EnumAppSide.Server)
      {
        entity.Api.Event.UnregisterGameTickListener(_serverTickListenerId);
      }
      else
      {
        // Clean up the client particle emitter
        entity.Api.Event.UnregisterGameTickListener(_clientTickListenerId);
      }
    }
  }
}