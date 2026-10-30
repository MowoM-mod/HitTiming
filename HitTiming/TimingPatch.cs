using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace HitTiming
{
    internal static class TimingPatch
    {
        private static float _cachedNjs;

        private static readonly Dictionary<Type, Dictionary<string, MemberInfo?>>
            MemberCache =
                new Dictionary<Type, Dictionary<string, MemberInfo?>>();

        private static readonly Dictionary<Type, MethodInfo?>
            DifficultyDataMethodCache =
                new Dictionary<Type, MethodInfo?>();

        private static readonly Dictionary<Type, MethodInfo?>
            DefaultNjsMethodCache =
                new Dictionary<Type, MethodInfo?>();

        internal static MethodBase? FindLevelRefreshMethod()
        {
            var type = AccessTools.TypeByName("StandardLevelDetailView");

            if (type == null)
                return null;

            return AccessTools.Method(type, "RefreshContent");
        }

        internal static void LevelRefreshPostfix(object __instance)
        {
            _cachedNjs = 0f;

            if (__instance == null)
                return;

            try
            {
                var beatmapKey = ReadMember(__instance, "beatmapKey");
                var beatmapLevel = ReadMember(__instance, "_beatmapLevel");

                if (beatmapKey == null || beatmapLevel == null)
                    return;

                var characteristic =
                    ReadMember(beatmapKey, "beatmapCharacteristic");

                var difficulty =
                    ReadMember(beatmapKey, "difficulty");

                if (characteristic == null || difficulty == null)
                    return;

                var basicData = GetDifficultyBeatmapData(
                    beatmapLevel,
                    characteristic,
                    difficulty);

                var njs = basicData != null
                    ? Convert.ToSingle(
                        ReadMember(basicData, "noteJumpMovementSpeed") ?? 0f)
                    : 0f;

                if (!IsValidNjs(njs))
                    njs = GetDefaultNoteJumpMovementSpeed(difficulty);

                if (IsValidNjs(njs))
                    _cachedNjs = njs;
            }
            catch
            {
                _cachedNjs = 0f;
            }
        }

        private static object? GetDifficultyBeatmapData(
            object beatmapLevel,
            object characteristic,
            object difficulty)
        {
            var type = beatmapLevel.GetType();

            if (!DifficultyDataMethodCache.TryGetValue(type, out var method))
            {
                const BindingFlags flags =
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic;

                method = null;

                for (var current = type;
                     current != null;
                     current = current.BaseType)
                {
                    method = current.GetMethods(flags)
                        .FirstOrDefault(candidate =>
                        {
                            if (candidate.Name != "GetDifficultyBeatmapData")
                                return false;

                            var parameters = candidate.GetParameters();

                            return parameters.Length == 2 &&
                                   parameters[0].ParameterType.IsInstanceOfType(characteristic) &&
                                   parameters[1].ParameterType.IsInstanceOfType(difficulty);
                        });

                    if (method != null)
                        break;
                }

                DifficultyDataMethodCache[type] = method;
            }

            return method?.Invoke(
                beatmapLevel,
                new[] { characteristic, difficulty });
        }

        private static float GetDefaultNoteJumpMovementSpeed(
            object difficulty)
        {
            var type = difficulty.GetType();

            if (!DefaultNjsMethodCache.TryGetValue(type, out var method))
            {
                method = FindDefaultNjsMethod(type);
                DefaultNjsMethodCache[type] = method;
            }

            if (method == null)
                return 0f;

            var result = method.Invoke(null, new[] { difficulty });

            return result != null
                ? Convert.ToSingle(result)
                : 0f;
        }

        private static MethodInfo? FindDefaultNjsMethod(Type difficultyType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types
                        .Where(type => type != null)
                        .Cast<Type>()
                        .ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    const BindingFlags flags =
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;

                    foreach (var method in type.GetMethods(flags))
                    {
                        if (method.Name != "DefaultNoteJumpMovementSpeed")
                            continue;

                        var parameters = method.GetParameters();

                        if (parameters.Length == 1 &&
                            parameters[0].ParameterType == difficultyType)
                            return method;
                    }
                }
            }

            return null;
        }

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var baseType = AccessTools.TypeByName("FlyingScoreEffect");
            if (baseType == null)
                yield break;

            foreach (var method in FindEntryPoints(baseType))
                yield return method;

            var hsvType = FindHsvType();

            if (hsvType != null)
            {
                foreach (var method in FindEntryPoints(hsvType))
                    yield return method;
            }
        }

        internal static IEnumerable<MethodBase> FadeTargetMethods()
        {
            var baseType = AccessTools.TypeByName("FlyingScoreEffect");
            if (baseType == null)
                yield break;

            var methods = new HashSet<MethodBase>();

            foreach (var method in FindFadeMethodsInHierarchy(baseType))
            {
                if (methods.Add(method))
                    yield return method;
            }

            var hsvType = FindHsvType();

            if (hsvType != null)
            {
                foreach (var method in FindFadeMethodsInHierarchy(hsvType))
                {
                    if (methods.Add(method))
                        yield return method;
                }
            }
        }

        private static Type? FindHsvType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(
                    "HitScoreVisualizer.Models.HsvFlyingScoreEffect",
                    false))
                .FirstOrDefault(type => type != null);
        }

        private static IEnumerable<MethodBase> FindEntryPoints(Type type)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            foreach (var method in type.GetMethods(flags))
            {
                if (method.Name != "InitAndPresent")
                    continue;

                var parameters = method.GetParameters();

                if (parameters.Length != 4)
                    continue;

                if (parameters[1].ParameterType != typeof(float) ||
                    parameters[2].ParameterType != typeof(Vector3) ||
                    parameters[3].ParameterType != typeof(Color))
                    continue;

                if (!parameters[0].ParameterType.Name.Contains(
                    "IReadonlyCutScoreBuffer"))
                    continue;

                yield return method;
            }
        }

        private static IEnumerable<MethodBase> FindFadeMethodsInHierarchy(
            Type type)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            for (var current = type;
                 current != null;
                 current = current.BaseType)
            {
                foreach (var method in current.GetMethods(flags))
                {
                    if (method.Name != "ManualUpdate")
                        continue;

                    var parameters = method.GetParameters();

                    if (parameters.Length != 1 ||
                        parameters[0].ParameterType != typeof(float))
                        continue;

                    if (method.IsAbstract)
                        continue;

                    try
                    {
                        if (method.GetMethodBody() == null)
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        internal static void Postfix(
            object __instance,
            object __0,
            float __1,
            Vector3 __2,
            Color __3)
        {
            var config = TimingConfiguration.Current;

            if (!config.Enabled || __instance == null || __0 == null)
                return;

            try
            {
                var cutInfo = ReadMember(__0, "noteCutInfo");
                if (cutInfo == null)
                    return;

                var noteData = ReadMember(cutInfo, "noteData");

                if (config.NormalNotesOnly && noteData != null)
                {
                    var gameplayType = ReadMember(noteData, "gameplayType");

                    if (gameplayType != null &&
                        !string.Equals(
                            gameplayType.ToString(),
                            "Normal",
                            StringComparison.Ordinal))
                        return;
                }

                var rawDeviation =
                    Convert.ToSingle(ReadMember(cutInfo, "timeDeviation"));

                var cutPoint = ReadMember(cutInfo, "cutPoint");
                var cutPointZ = cutPoint is Vector3 point ? point.z : 0f;

                var njs = GetNoteJumpMovementSpeed();

                // Mantiene invariata la formula di correzione.
                var correctedDeviation = rawDeviation;

                if (njs > 0f &&
                    !float.IsNaN(njs) &&
                    !float.IsInfinity(njs))
                {
                    correctedDeviation -=
                        (cutPointZ - config.CorrectionDistance) / njs;
                }

                var deviationMs = correctedDeviation * 1000f;

                if (Mathf.Abs(deviationMs) < 0.5f)
                    deviationMs = 0f;

                var sourceText =
                    ReadMember(__instance, "_text") as TextMeshPro;

                if (sourceText == null)
                    return;

                var effectComponent = __instance as Component;
                if (effectComponent == null)
                    return;

                var color =
                    config.GetColor(Mathf.Abs(deviationMs));

                var rotation = sourceText.transform.rotation;

                var localOffset = new Vector3(
                    config.OffsetX,
                    config.OffsetY,
                    config.OffsetZ);

                var position = __2 + rotation * localOffset;

                var textValue = $"{deviationMs:+0;-0;0} ms";

                TimingTextLifetime.Show(
                    effectComponent.gameObject,
                    sourceText,
                    position,
                    rotation,
                    textValue,
                    config.FontSizeMultiplier,
                    color);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[HitTiming] Could not display timing text: {exception.Message}");
            }
        }

        internal static void FadePostfix(object __instance)
        {
            if (__instance == null)
                return;

            try
            {
                var component = __instance as Component;
                if (component == null)
                    return;

                var timingText =
                    component.GetComponent<TimingTextLifetime>();

                timingText?.SyncFade();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[HitTiming] Could not synchronize text fade: {exception.Message}");
            }
        }

        private static float GetNoteJumpMovementSpeed()
        {
            return IsValidNjs(_cachedNjs) ? _cachedNjs : 0f;
        }

        private static bool IsValidNjs(float njs)
        {
            return njs > 0f &&
                   !float.IsNaN(njs) &&
                   !float.IsInfinity(njs);
        }

        private static object? ReadMember(object instance, string name)
        {
            var type = instance.GetType();

            if (!MemberCache.TryGetValue(type, out var typeCache))
            {
                typeCache = new Dictionary<string, MemberInfo?>(
                    StringComparer.Ordinal);

                MemberCache.Add(type, typeCache);
            }

            if (!typeCache.TryGetValue(name, out var member))
            {
                member = FindMember(type, name);
                typeCache.Add(name, member);
            }

            if (member is PropertyInfo property)
                return property.GetValue(instance, null);

            if (member is FieldInfo field)
                return field.GetValue(instance);

            return null;
        }

        private static MemberInfo? FindMember(Type type, string name)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            for (var current = type;
                 current != null;
                 current = current.BaseType)
            {
                var property = current.GetProperty(name, flags);
                if (property != null)
                    return property;

                var field = current.GetField(name, flags);
                if (field != null)
                    return field;
            }

            return null;
        }
    }
}