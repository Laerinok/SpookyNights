using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace SpookyNights
{
    public class EntityBehaviorProximityWarning : EntityBehavior
    {
        private string soundPath = "spookynights:sounds/creature/bear/spectral_bear_warning";
        private float soundVolume = 1.0f;
        private float checkInterval = 2.0f;
        private float cooldownMs = 10000f;

        private float accumulator = 0f;
        private long lastSoundTime = 0;
        private ICoreClientAPI? capi;

        public EntityBehaviorProximityWarning(Entity entity) : base(entity) { }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            capi = entity.World.Api as ICoreClientAPI;

            soundPath = attributes["sound"].AsString("spookynights:sounds/creature/bear/spectral_bear_warning");
            soundVolume = attributes["volume"].AsFloat(1.0f);
            cooldownMs = attributes["cooldownMs"].AsFloat(10000f);
        }

        public override void OnGameTick(float deltaTime)
        {
            if (entity.World.Side != EnumAppSide.Client || capi == null) return;

            if (ConfigManager.ClientConf != null && !ConfigManager.ClientConf.EnableBossWarningSound) return;

            if (!entity.Code.Path.Contains("giant")) return;

            accumulator += deltaTime;
            if (accumulator < checkInterval) return;
            accumulator = 0f;

            long currentTime = entity.World.ElapsedMilliseconds;
            if (currentTime - lastSoundTime < cooldownMs) return;

            var playerEntity = capi.World.Player.Entity;
            if (playerEntity == null || !playerEntity.Alive) return;

            double distance = entity.Pos.DistanceTo(playerEntity.Pos);

            double maxRange = ConfigManager.ClientConf!.BossWarningMaxRange;
            double minRange = ConfigManager.ClientConf.BossWarningMinRange;


            if (distance <= maxRange && distance > minRange)
            {
                PlayClientSound();
                lastSoundTime = currentTime;
            }
        }

        private void PlayClientSound()
        {
            if (capi == null) return;

            entity.World.PlaySoundAt(
                new AssetLocation(soundPath),
                entity.Pos.X,
                entity.Pos.Y,
                entity.Pos.Z,
                null,
                false,
                32,
                soundVolume
            );
        }

        public override string PropertyName()
        {
            return "proximitywarning";
        }
    }
}