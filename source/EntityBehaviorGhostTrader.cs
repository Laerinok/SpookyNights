using System;
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
    private long _tickListenerId;

    private Cuboidf? _origHitbox;
    private Cuboidf? _origSelection;

    public EntityBehaviorGhostTrader(Entity entity) : base(entity) { }

    public override string PropertyName() => "ghosttrader";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
      base.Initialize(properties, attributes);

      _origHitbox = entity.CollisionBox.Clone();
      _origSelection = entity.SelectionBox.Clone();

      if (entity.Api.Side == EnumAppSide.Server)
      {
        _config = ConfigManager.ServerConf;

        // L'ANCRE : On enregistre la position de surface UNE SEULE FOIS à sa création.
        // On utilise WatchedAttributes pour la partager avec le client et le brouillard.
        if (!entity.WatchedAttributes.HasAttribute("origY"))
        {
          entity.WatchedAttributes.SetDouble("origX", entity.Pos.X);
          entity.WatchedAttributes.SetDouble("origY", entity.Pos.Y);
          entity.WatchedAttributes.SetDouble("origZ", entity.Pos.Z);
        }

        // Délai de 2 secondes au chargement du chunk pour laisser le moteur de lumière se calculer
        entity.Api.Event.RegisterCallback((dt) => {
          if (entity.Alive) SyncGhostStatus(true);
        }, 2000);

        // Boucle de vérification toutes les 5 secondes
        _tickListenerId = entity.Api.Event.RegisterGameTickListener(_ => SyncGhostStatus(false), 5000);
      }
      else
      {
        entity.WatchedAttributes.RegisterModifiedListener("isHidden", () => UpdateClientState());
        UpdateClientState();
      }
    }

    private void UpdateClientState()
    {
      bool isHidden = entity.WatchedAttributes.GetBool("isHidden", false);
      if (isHidden)
      {
        // Disparition totale des boîtes de collision (invisible et intouchable, même sous terre)
        entity.CollisionBox.Set(0, 0, 0, 0, 0, 0);
        entity.SelectionBox.Set(0, 0, 0, 0, 0, 0);
      }
      else
      {
        // Restauration côté client
        if (_origHitbox != null) entity.CollisionBox.Set(_origHitbox);
        if (_origSelection != null) entity.SelectionBox.Set(_origSelection);
      }
    }

    private void SyncGhostStatus(bool isInit)
    {
      if (_config == null || !_config.UseTimeBasedSpawning || !_config.SpawnOnlyAtNight) return;

      bool isNight = IsNightTime(entity.Api, _config);
      bool isHidden = entity.WatchedAttributes.GetBool("isHidden", false);

      if (isNight && isHidden)
      {
        // LA NUIT TOMBE : Réveil du marchand
        entity.State = EnumEntityState.Active;

        // On lit l'ancre fixe
        double origX = entity.WatchedAttributes.GetDouble("origX", entity.Pos.X);
        double origY = entity.WatchedAttributes.GetDouble("origY", entity.Pos.Y);
        double origZ = entity.WatchedAttributes.GetDouble("origZ", entity.Pos.Z);

        // On le remonte exactement à la surface
        entity.TeleportTo(new Vec3d(origX, origY, origZ));

        entity.WatchedAttributes.SetBool("isHidden", false);
        entity.WatchedAttributes.MarkPathDirty("isHidden");

        if (!isInit) SpawnGhostParticles(true);
      }
      else if (!isNight && !isHidden)
      {
        // LE JOUR SE LÈVE : Hibernation absolue
        if (!isInit) SpawnGhostParticles(false);

        // On lit l'ancre pour être certain de s'enterrer par rapport à la VRAIE surface
        double origY = entity.WatchedAttributes.GetDouble("origY", entity.Pos.Y);

        // On l'enterre à -2 blocs pour que le moteur physique ne le remonte pas
        double hiddenY = Math.Max(1.0, origY - 2.0);
        entity.TeleportTo(new Vec3d(entity.Pos.X, hiddenY, entity.Pos.Z));

        entity.Pos.Motion.Set(0, 0, 0);
        entity.WatchedAttributes.SetBool("isHidden", true);
        entity.WatchedAttributes.MarkPathDirty("isHidden");

        // ON DÉSACTIVE TOUT DE SUITE
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
        damage = 0;
        return;
      }

      if (damageSource.Type == EnumDamageType.Heal) return;
      if (damageSource.SourceEntity is EntityPlayer entityPlayer)
      {
        IServerPlayer? serverPlayer = entityPlayer.Player as IServerPlayer;
        damage = 0;

        double currentTime = entity.Api.World.Calendar.TotalHours;
        double lastStrikeTime = entity.Attributes.GetDouble("lastStrikeTime", 0);
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
              entityPlayer.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.PiercingAttack, SourceEntity = entity }, 100f);
              entity.Api.World.PlaySoundAt(new AssetLocation("spookynights:creature/trader/growl1"), entity);
              entity.Attributes.SetInt("strikeCount", 0);
              break;
          }
        }
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

    private bool IsNightTime(ICoreAPI api, ServerConfig config) => api.World.Calendar.GetDayLightStrength(entity.Pos.X, entity.Pos.Z) < 0.85f;

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
      if (entity.Api.Side == EnumAppSide.Server)
        entity.Api.Event.UnregisterGameTickListener(_tickListenerId);
    }
  }
}