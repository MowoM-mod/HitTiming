using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Logging;

namespace HitTiming
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public sealed class Plugin
    {
        private Harmony? _harmony;
        private Logger? _logger;

        [Init]
        public void Init(Logger logger)
        {
            _logger = logger;

            TimingConfiguration.Load(
                Path.Combine(
                    Environment.CurrentDirectory,
                    "UserData",
                    "HitTiming.json"),
                logger);

            _harmony = new Harmony("com.monolith.hittiming");

            var spawnControllerPatched = false;

            var spawnControllerType =
                AccessTools.TypeByName("BeatmapObjectSpawnController");

            var spawnControllerStart = spawnControllerType?.GetMethod(
                "Start",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly,
                null,
                Type.EmptyTypes,
                null);

            if (spawnControllerStart != null)
            {
                _harmony.Patch(
                    spawnControllerStart,
                    postfix: new HarmonyMethod(
                        typeof(TimingPatch),
                        nameof(TimingPatch.SpawnControllerStartPostfix)));

                spawnControllerPatched = true;
            }

            var entryPointsPatched = 0;

            foreach (var method in TimingPatch.TargetMethods())
            {
                _harmony.Patch(
                    method,
                    postfix: new HarmonyMethod(
                        typeof(TimingPatch),
                        nameof(TimingPatch.Postfix)));

                entryPointsPatched++;
            }

            var fadeMethodsPatched = 0;

            foreach (var method in TimingPatch.FadeTargetMethods())
            {
                _harmony.Patch(
                    method,
                    postfix: new HarmonyMethod(
                        typeof(TimingPatch),
                        nameof(TimingPatch.FadePostfix)));

                fadeMethodsPatched++;
            }

            logger.Info(
                $"HitTiming initialized. " +
                $"Spawn controller preload patched: {spawnControllerPatched}. " +
                $"Timing entry points patched: {entryPointsPatched}. " +
                $"Fade methods patched: {fadeMethodsPatched}.");

            if (!spawnControllerPatched)
            {
                logger.Warn(
                    "HitTiming could not find a declared " +
                    "BeatmapObjectSpawnController.Start() method. " +
                    "NJS will use the fallback lookup on the first hit.");
            }

            if (entryPointsPatched == 0)
            {
                logger.Error(
                    "HitTiming could not find a supported FlyingScoreEffect.InitAndPresent entry point.");
            }

            if (fadeMethodsPatched == 0)
            {
                logger.Warn(
                    "HitTiming could not find a supported ManualUpdate fade method.");
            }
        }

        [OnExit]
        public void OnExit()
        {
            _harmony?.UnpatchSelf();
            _logger?.Info("HitTiming stopped.");
        }
    }
}