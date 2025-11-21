using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace SpookyNights
{
    public class EntityBehaviorProximityWarning : EntityBehavior
    {
        private float warningRange = 35f;
        private string soundPath = "spookynights:sounds/creature/bear/bear-giant-growl";
        private float soundVolume = 1.0f;
        private float checkInterval = 2.0f;
        private float cooldownMs = 10000f;

        private float accumulator = 0f;
        private long lastSoundTime = 0;

        public EntityBehaviorProximityWarning(Entity entity) : base(entity) { }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            warningRange = attributes["range"].AsFloat(35f);
            soundPath = attributes["sound"].AsString("spookynights:sounds/creature/bear/bear-giant-growl");
            soundVolume = attributes["volume"].AsFloat(1.0f);
            cooldownMs = attributes["cooldownMs"].AsFloat(10000f);
        }

        public override void OnGameTick(float deltaTime)
        {
            // Filter: Only allow this behavior on the Giant variant
            if (!entity.Code.Path.Contains("giant")) return;

            if (entity.World.Side != EnumAppSide.Server) return;

            accumulator += deltaTime;

            if (accumulator < checkInterval) return;
            accumulator = 0f;

            long currentTime = entity.World.ElapsedMilliseconds;
            if (currentTime - lastSoundTime < cooldownMs) return;

            EntityPlayer? nearestPlayer = entity.World.GetNearestEntity(
                entity.ServerPos.XYZ,
                warningRange,
                warningRange,
                (e) => e is EntityPlayer && e.Alive
            ) as EntityPlayer;

            if (nearestPlayer != null)
            {
                PlayWarningSound();
                lastSoundTime = currentTime;
            }
        }

        private void PlayWarningSound()
        {
            entity.World.PlaySoundAt(
                new AssetLocation(soundPath),
                entity.ServerPos.X,
                entity.ServerPos.Y,
                entity.ServerPos.Z,
                null,
                false,
                warningRange,
                soundVolume
            );
        }

        public override string PropertyName()
        {
            return "proximitywarning";
        }
    }
}