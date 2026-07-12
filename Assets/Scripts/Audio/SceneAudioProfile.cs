using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneAudioProfile : MonoBehaviour
{
    [Header("Scene Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool loopMusic = true;

    [Header("Scene Ambient")]
    [SerializeField] private AudioClip ambientClip;
    [SerializeField] private bool loopAmbient = true;

    public AudioClip MusicClip => musicClip;
    public AudioClip AmbientClip => ambientClip;
    public bool LoopMusic => loopMusic;
    public bool LoopAmbient => loopAmbient;

    private void Start()
    {
        GameAudioManager.ApplySceneProfile(this);
    }
}
