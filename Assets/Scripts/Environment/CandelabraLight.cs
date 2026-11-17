using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class CandelabraLight : MonoBehaviour
{
    [SerializeField, Min(0f)] private float brightness = 0.45f;
    [SerializeField, Min(0.1f)] private float radius = 4f;
    private Light2D candleLight;
    private MatchStats match;
    private float elapsed;
    private float phase;

    public void Initialize(MatchStats stats)
    {
        match = stats;
        phase = Mathf.Repeat(transform.position.x * 0.73f + transform.position.y * 0.37f, 30f);
        var source = new GameObject("Warm candle light");
        source.transform.SetParent(transform, false);
        source.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        candleLight = source.AddComponent<Light2D>();
        candleLight.lightType = Light2D.LightType.Point;
        candleLight.blendStyleIndex = 0;
        candleLight.color = new Color(1f, 0.66f, 0.3f);
        candleLight.overlapOperation = Light2D.OverlapOperation.Additive;
        candleLight.lightOrder = 1;
        candleLight.pointLightInnerRadius = 0.3f;
        candleLight.pointLightOuterRadius = radius;
        candleLight.intensity = brightness;
        AnimateLight();
    }

    private void Update()
    {
        if (candleLight == null || (match != null && (match.IsPaused || match.Finished))) return;
        elapsed += Time.deltaTime;
        AnimateLight();
    }

    private void AnimateLight()
    {
        // Smooth independent flicker, without consuming the gameplay random sequence.
        float flicker = Mathf.PerlinNoise(phase, elapsed * 1.8f) * 2f - 1f;
        candleLight.intensity = brightness * (1f + flicker * 0.12f);
    }
}