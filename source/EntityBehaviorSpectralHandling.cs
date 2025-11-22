using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace SpookyNights
{
    public class EntityBehaviorSpectralHandling : EntityBehavior
    {
        private List<string> activeStatKeys = new List<string>();
        private float checkInterval = 0.2f; // Run check 5 times per second
        private float accumulator = 0f;

        public EntityBehaviorSpectralHandling(Entity entity) : base(entity) { }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void OnGameTick(float deltaTime)
        {
            accumulator += deltaTime;
            if (accumulator < checkInterval) return;
            accumulator = 0f;

            if (entity is not EntityPlayer player) return;

            // Check right hand item
            ItemSlot? activeSlot = player.RightHandItemSlot;
            ItemStack? stack = activeSlot?.Itemstack;

            if (stack != null)
            {
                JsonObject? modifiers = stack.ItemAttributes?["statModifiers"];

                if (modifiers != null && modifiers.Exists)
                {
                    ApplyStats(modifiers);
                }
                else
                {
                    RemoveStats();
                }
            }
            else
            {
                RemoveStats();
            }
        }

        private void ApplyStats(JsonObject modifiers)
        {
            List<string> currentKeys = new List<string>();

            // Safely cast to JObject to iterate over keys
            if (modifiers != null && modifiers.Token is JObject tokenAsObject)
            {
                foreach (var entry in tokenAsObject)
                {
                    // Convert key to lowercase for engine compatibility (e.g. "walkSpeed" -> "walkspeed")
                    string statName = entry.Key.ToLowerInvariant();

                    try
                    {
                        float? val = entry.Value?.ToObject<float>();

                        if (val.HasValue)
                        {
                            // "spookynights-held" is the unique tag used to identify and remove these stats later
                            entity.Stats.Set(statName, "spookynights-held", val.Value, true);
                            currentKeys.Add(statName);
                        }
                    }
                    catch
                    {
                        // Ignore invalid values
                    }
                }
            }

            activeStatKeys = currentKeys;
        }

        private void RemoveStats()
        {
            if (activeStatKeys == null || activeStatKeys.Count == 0) return;

            foreach (string statName in activeStatKeys)
            {
                entity.Stats.Remove(statName, "spookynights-held");
            }
            activeStatKeys.Clear();
        }

        public override string PropertyName() => "spectralhandling";
    }
}