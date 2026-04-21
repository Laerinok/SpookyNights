using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace SpookyNights
{
  public class SystemCemeteryFog : ModSystem
  {
    private ICoreClientAPI? capi;
    private AmbientModifier? cemeteryFog;
    private float currentWeight = 0f;
    private bool isRegistered = false;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
      capi = api;
      cemeteryFog = new AmbientModifier().EnsurePopulated();

      // --- FINAL CONFIGURATION ---
      cemeteryFog.FogDensity.Value = 0.0f;   // Clear air above the fog layer (0)
      cemeteryFog.FlatFogDensity.Value = -5.0f; // Ground fog layer density

      // Default color (Plain / Cemetery): Bluish spectral gray
      cemeteryFog.FogColor.Value = new float[] { 0.65f, 0.65f, 0.75f };
      cemeteryFog.AmbientColor.Value = new float[] { 0.15f, 0.15f, 0.25f };

      cemeteryFog.FogMin.Value = 0f;

      // Initialize weights
      cemeteryFog.FogDensity.Weight = 0f;
      cemeteryFog.FogColor.Weight = 0f;
      cemeteryFog.AmbientColor.Weight = 0f;
      cemeteryFog.FlatFogDensity.Weight = 0f;
      cemeteryFog.FlatFogYPos.Weight = 0f;

      api.Event.RegisterGameTickListener(OnGameTick, 20); // 20ms for a smooth transition
    }

    private void OnGameTick(float dt)
    {
      if (capi == null || capi.World?.Player == null || capi.IsGamePaused) return;

      if (!isRegistered)
      {
        capi.Ambient.CurrentModifiers["spookynights:cemeteryfog"] = cemeteryFog;
        isRegistered = true;
        return;
      }

      UpdateLogic(dt);

      // SMOOTH TRANSITION: All parameters follow currentWeight
      if (cemeteryFog != null)
      {
        cemeteryFog.FogDensity.Weight = currentWeight;
        cemeteryFog.FogColor.Weight = currentWeight;
        cemeteryFog.AmbientColor.Weight = currentWeight;
        cemeteryFog.FlatFogDensity.Weight = currentWeight;
        cemeteryFog.FlatFogYPos.Weight = currentWeight;
      }
    }

    private void UpdateLogic(float dt)
    {
      EntityPlayer player = capi!.World.Player.Entity;
      Entity? nearestTrader = null;

      float activeRadius = 22f; // Base radius
      double minDistSq = 999999;

      // 1. Default colors (Plain / Cemetery)
      float[] targetFogColor = new float[] { 0.65f, 0.65f, 0.75f };
      float[] targetAmbientColor = new float[] { 0.15f, 0.15f, 0.25f };

      foreach (var entity in capi.World.LoadedEntities.Values)
      {
        if (entity.Code != null && entity.Code.Path.Contains("trader-cursed"))
        {
          float radiusForThisEntity = 25f;
          float[] fogColorForThis = new float[] { 0.65f, 0.65f, 0.75f };
          float[] ambientForThis = new float[] { 0.15f, 0.15f, 0.25f };

          // --- SPECIFIC BIOMES ---

          // TEMPERATE FOREST (Whispering Well)
          if (entity.Code.Path.Contains("temperateforest"))
          {
            radiusForThisEntity = 35f;
            fogColorForThis = new float[] { 0.4f, 0.5f, 0.4f };    // Dark moss green
            ambientForThis = new float[] { 0.05f, 0.15f, 0.05f };  // Greenish glow
          }
          // SWAMP (Murmuring Mire)
          else if (entity.Code.Path.Contains("swamp"))
          {
            radiusForThisEntity = 30f;
            fogColorForThis = new float[] { 0.35f, 0.45f, 0.4f }; // Brackish water green
            ambientForThis = new float[] { 0.05f, 0.12f, 0.1f };  // Sickly glow
          }
          // TROPICAL JUNGLE (Hollow-Heart)
          else if (entity.Code.Path.Contains("tropicalforest"))
          {
            radiusForThisEntity = 35f;
            fogColorForThis = new float[] { 0.25f, 0.35f, 0.15f }; // Very dark green miasma
            ambientForThis = new float[] { 0.05f, 0.1f, 0.05f };   // Gloom
          }
          // ARID / DESERTS / SAVANNA (Dust-Walkers)
          else if (entity.Code.Path.Contains("arid"))
          {
            radiusForThisEntity = 40f; // Large radius for sandstorm effect
            fogColorForThis = new float[] { 0.75f, 0.6f, 0.4f };  // Ochre / Sand
            ambientForThis = new float[] { 0.2f, 0.15f, 0.1f };   // Stifling heat
          }
          // ARCTIC (Ice-Veined)
          else if (entity.Code.Path.Contains("arctic"))
          {
            radiusForThisEntity = 20f; // Large radius for blizzard
            fogColorForThis = new float[] { 0.8f, 0.9f, 0.95f };  // Icy cyan / White
            ambientForThis = new float[] { 0.15f, 0.2f, 0.3f };   // Cold glow
          }
          // WANDERER (Wandering Camp)
          else if (entity.Code.Path.Contains("wanderer"))
          {
            radiusForThisEntity = 15f; // Very close to player
            fogColorForThis = new float[] { 0.5f, 0.5f, 0.5f };    // Neutral gray
            ambientForThis = new float[] { 0.1f, 0.1f, 0.15f };    // Dark
          }

          double distSq = entity.Pos.XYZ.SquareDistanceTo(player.Pos.XYZ);

          if (distSq < (radiusForThisEntity * radiusForThisEntity) && distSq < minDistSq)
          {
            minDistSq = distSq;
            nearestTrader = entity;
            activeRadius = radiusForThisEntity;
            targetFogColor = fogColorForThis;
            targetAmbientColor = ambientForThis;
          }
        }
      }

      float targetWeight = 0f;
      if (nearestTrader != null)
      {
        float dist = (float)Math.Sqrt(minDistSq);
        targetWeight = 1f - (dist / activeRadius);

        // Immediate color update (transition is handled via the 'weight' property)
        cemeteryFog!.FogColor.Value = targetFogColor;
        cemeteryFog!.AmbientColor.Value = targetAmbientColor;

        float surfaceY = (float)nearestTrader.WatchedAttributes.GetDouble("origY", nearestTrader.Pos.Y);
        cemeteryFog!.FlatFogYPos.Value = surfaceY + 1.0f;
      }

      targetWeight = GameMath.Clamp(targetWeight, 0f, 1f);

      if (currentWeight < targetWeight)
        currentWeight = Math.Min(targetWeight, currentWeight + dt * 0.5f);
      else
        currentWeight = Math.Max(targetWeight, currentWeight - dt * 0.5f);
    }
  }
}