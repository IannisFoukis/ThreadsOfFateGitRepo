using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenStress : MonoBehaviour
{
    public Volume volume;

    Vignette vignette;
    ChromaticAberration chroma;

    void Start()
    {
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chroma);
    }

    void Update()
    {
        var coord = FindAnyObjectByType<EncounterCoordinator>();
        if (coord == null)
            return;

        float stress = coord.phalanxState == EncounterCoordinator.PhalanxState.Collapse ? 1f :
                       coord.phalanxState == EncounterCoordinator.PhalanxState.Encircle ? 0.4f : 0f;

        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, stress * 0.35f, Time.deltaTime * 3f);
        chroma.intensity.value = Mathf.Lerp(chroma.intensity.value, stress * 0.15f, Time.deltaTime * 3f);
    }
}