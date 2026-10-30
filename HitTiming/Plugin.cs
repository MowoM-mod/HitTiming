using System;
using System.IO;
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

            var levelRefreshMethod =
                TimingPatch.FindLevelRefreshMethod();

            var levelRefreshPatched = false;

            if (levelRefreshMethod != null)
            {
                _harmony.Patch(
                    levelRefreshMethod,
                    postfix: new HarmonyMethod(
                        typeof(TimingPatch),
                        nameof(TimingPatch.LevelRefreshPostfix)));

                levelRefreshPatched = true;
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
                $"Level refresh patched: {levelRefreshPatched}. " +
                $"Timing entry points patched: {entryPointsPatched}. " +
                $"Fade methods patched: {fadeMethodsPatched}.");

            if (!levelRefreshPatched)
            {
                logger.Warn(
                    "HitTiming could not find StandardLevelDetailView.RefreshContent. " +
                    "NJS correction will remain unavailable.");
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