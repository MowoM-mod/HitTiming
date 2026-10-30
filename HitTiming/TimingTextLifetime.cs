using TMPro;
using UnityEngine;

namespace HitTiming
{
    internal sealed class TimingTextLifetime : MonoBehaviour
    {
        private TextMeshPro? _sourceText;
        private TextMeshPro? _timingText;
        private GameObject? _timingObject;

        private Vector3 _startPosition;
        private float _elapsed;
        private float _startAlpha;
        private int _lastSyncFrame = -1;

        internal static void Show(
            GameObject effectObject,
            TextMeshPro sourceText,
            Vector3 position,
            Quaternion rotation,
            string value,
            float fontSizeMultiplier,
            Color color)
        {
            var lifetime =
                effectObject.GetComponent<TimingTextLifetime>();

            if (lifetime == null)
                lifetime = effectObject.AddComponent<TimingTextLifetime>();

            lifetime.ShowInternal(
                sourceText,
                position,
                rotation,
                value,
                fontSizeMultiplier,
                color);
        }

        private void ShowInternal(
            TextMeshPro sourceText,
            Vector3 position,
            Quaternion rotation,
            string value,
            float fontSizeMultiplier,
            Color color)
        {
            if (_timingText == null ||
                _timingObject == null ||
                _sourceText != sourceText)
            {
                CreateText(sourceText);
            }

            if (_timingText == null || _timingObject == null)
                return;

            _timingText.text = value;
            _timingText.richText = false;
            _timingText.enableWordWrapping = false;
            _timingText.overflowMode = TextOverflowModes.Overflow;
            _timingText.fontSize =
                sourceText.fontSize * Mathf.Max(0.1f, fontSizeMultiplier);

            _timingText.color = color;
            _startAlpha = color.a;

            _timingObject.transform.rotation = rotation;
            _timingObject.transform.position = position;

            _startPosition = position;
            _elapsed = 0f;
            _lastSyncFrame = -1;

            _timingObject.SetActive(true);
        }

        private void CreateText(TextMeshPro sourceText)
        {
            if (_timingObject != null)
                Destroy(_timingObject);

            _sourceText = sourceText;

            var clone = Instantiate(sourceText.gameObject);
            clone.name = "HitTimingText";
            clone.transform.SetParent(null, true);
            clone.SetActive(false);

            var text = clone.GetComponent<TextMeshPro>();

            if (text == null)
            {
                Destroy(clone);
                _timingObject = null;
                _timingText = null;
                return;
            }

            _timingObject = clone;
            _timingText = text;

            var fadeRunner = clone.AddComponent<TimingTextFadeRunner>();
            fadeRunner.Initialize(this);
        }

        internal void SyncFade()
        {
            if (_lastSyncFrame == Time.frameCount)
                return;

            _lastSyncFrame = Time.frameCount;

            if (_sourceText == null ||
                _timingText == null ||
                _timingObject == null ||
                !_timingObject.activeSelf)
                return;

            _elapsed += Time.deltaTime;

            var config = TimingConfiguration.Current;
            var holdTime = Mathf.Max(0f, config.HoldTime);
            var fadeTime = Mathf.Max(0f, config.FadeTime);
            var fallSpeed = Mathf.Max(0f, config.FallSpeed);

            var position = _startPosition;
            var fallDistance = fallSpeed * _elapsed;

            switch (config.FallAxis.ToUpperInvariant())
            {
                case "X":
                    position.x -= fallDistance;
                    break;

                case "Z":
                    position.z += fallDistance;
                    break;

                default:
                    position.y -= fallDistance;
                    break;
            }

            _timingObject.transform.position = position;
            if (_elapsed <= holdTime)
            {
                _timingText.alpha = _startAlpha;
            }
            else if (fadeTime > 0f)
            {
                var fadeProgress = Mathf.Clamp01(
                    (_elapsed - holdTime) / fadeTime);

                _timingText.alpha = _startAlpha * (1f - fadeProgress);
            }
            else
            {
                _timingText.alpha = 0f;
            }

            if (_elapsed >= holdTime + fadeTime)
                _timingObject.SetActive(false);
        }


        private void OnDestroy()
        {
            if (_timingObject != null)
                Destroy(_timingObject);

            _timingObject = null;
            _timingText = null;
            _sourceText = null;
        }
    }
    internal sealed class TimingTextFadeRunner : MonoBehaviour
    {
        private TimingTextLifetime? _lifetime;

        internal void Initialize(TimingTextLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        private void Update()
        {
            _lifetime?.SyncFade();
        }
    }
}