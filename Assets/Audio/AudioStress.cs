using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioStress : MonoBehaviour
{
    public AudioClip heartbeat;
    public float maxVolume = 0.7f;
    public float maxPitch = 1.2f;

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.clip = heartbeat;
        src.loop = true;
        src.playOnAwake = false;
    }

    void Update()
    {
        var coord = FindAnyObjectByType<EncounterCoordinator>();
        if (coord == null)
            return;

        float stress = GetStress(coord);

        if (stress > 0.1f)
        {
            if (!src.isPlaying)
                src.Play();

            src.volume = Mathf.Lerp(src.volume, stress * maxVolume, Time.deltaTime * 4f);
            src.pitch = Mathf.Lerp(src.pitch, 1f + stress * (maxPitch - 1f), Time.deltaTime * 4f);
        }
        else
        {
            src.volume = Mathf.Lerp(src.volume, 0f, Time.deltaTime * 3f);
            if (src.volume < 0.01f && src.isPlaying)
                src.Stop();
        }
    }

    float GetStress(EncounterCoordinator coord)
    {
        switch (coord.phalanxState)
        {
            case EncounterCoordinator.PhalanxState.Encircle:
                return 0.4f;
            case EncounterCoordinator.PhalanxState.Collapse:
                return 1f;
            default:
                return 0f;
        }
    }
}