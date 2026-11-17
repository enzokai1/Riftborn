using UnityEngine;

[DisallowMultipleComponent]
public sealed class HauntedClock : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float chimeVolume = 0.3f;
    private MatchStats match;
    private AudioSource audioSource;
    private AudioClip chime;
    private bool announcedBoss;
    private bool paused;

    public void Initialize(MatchStats stats)
    {
        match = stats;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        // This is an encounter cue: it remains audible across the entire room.
        audioSource.spatialBlend = 0f;
        audioSource.volume = chimeVolume;
        chime = CreateChime();
        audioSource.clip = chime;
    }

    private void Update()
    {
        if (match == null || audioSource == null) return;
        if (match.Finished)
        {
            audioSource.Stop();
            return;
        }
        if (match.IsPaused)
        {
            if (!paused) { audioSource.Pause(); paused = true; }
            return;
        }
        if (paused) { audioSource.UnPause(); paused = false; }
        if (announcedBoss || !match.BossSpawned) return;
        announcedBoss = true;
        audioSource.Play();
    }

    private static AudioClip CreateChime()
    {
        // Original synthesized bell; the art pack contains no bell recording.
        const int sampleRate = 44100;
        var samples = new float[sampleRate * 3];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)sampleRate;
            float attack = Mathf.Min(1f, t / 0.008f);
            float tail = Mathf.Clamp01((3f - t) / 0.2f);
            float fundamental = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 1.25f);
            float overtone = 0.4f * Mathf.Sin(2f * Mathf.PI * 587f * t) * Mathf.Exp(-t * 2.3f);
            float strike = 0.18f * Mathf.Sin(2f * Mathf.PI * 1184f * t) * Mathf.Exp(-t * 5f);
            samples[i] = (fundamental + overtone + strike) * attack * tail * 0.55f;
        }
        AudioClip clip = AudioClip.Create("Clock boss chime", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
        paused = false;
    }

    private void OnDestroy()
    {
        if (chime != null) Destroy(chime);
    }
}