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

      // --- CONFIGURATION FINALE ---
      cemeteryFog.FogDensity.Value = 0.0f;   // Air pur au-dessus de la nappe (0)
      cemeteryFog.FlatFogDensity.Value = -5.0f; // Ton réglage magique (-500)

      // Gris spectral bleuté
      cemeteryFog.FogColor.Value = new float[] { 0.65f, 0.65f, 0.75f };

      // LUMIÈRE DE NUIT : On ajoute une lueur pour voir la brume dans le noir
      cemeteryFog.AmbientColor.Value = new float[] { 0.15f, 0.15f, 0.25f };

      cemeteryFog.FogMin.Value = 0f;

      // Initialisation des poids
      cemeteryFog.FogDensity.Weight = 0f;
      cemeteryFog.FogColor.Weight = 0f;
      cemeteryFog.AmbientColor.Weight = 0f;
      cemeteryFog.FlatFogDensity.Weight = 0f;
      cemeteryFog.FlatFogYPos.Weight = 0f;

      api.Event.RegisterGameTickListener(OnGameTick, 20); // 20ms pour une transition fluide
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

      // TRANSITION DOUCE : Tous les paramètres suivent currentWeight
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
      float fogRadius = 22f;
      double minDistSq = fogRadius * fogRadius;

      foreach (var entity in capi.World.LoadedEntities.Values)
      {
        if (entity.Code != null && entity.Code.Path.Contains("trader-cursed"))
        {
          double distSq = entity.Pos.XYZ.SquareDistanceTo(player.Pos.XYZ);
          if (distSq < minDistSq)
          {
            minDistSq = distSq;
            nearestTrader = entity;
          }
        }
      }

      float targetWeight = 0f;
      if (nearestTrader != null)
      {
        float dist = (float)Math.Sqrt(minDistSq);
        targetWeight = 1f - (dist / fogRadius);

        bool isBuried = nearestTrader.WatchedAttributes.GetBool("isHidden", false);

        // --- RÉGLAGE DE LA HAUTEUR (NAPPE RAMPANTE) ---
        float yOffset = isBuried ? 3.0f : 1.0f;

        cemeteryFog!.FlatFogYPos.Value = (float)nearestTrader.Pos.Y + yOffset;
      }

      targetWeight = GameMath.Clamp(targetWeight, 0f, 1f);

      // Interpolation fluide (le brouillard met 2-3 sec à apparaître totalement)
      if (currentWeight < targetWeight)
        currentWeight = Math.Min(targetWeight, currentWeight + dt * 0.5f);
      else
        currentWeight = Math.Max(targetWeight, currentWeight - dt * 0.5f);
    }
  }
}