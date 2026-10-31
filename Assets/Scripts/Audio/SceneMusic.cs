using UnityEngine;

namespace Boruhan.Audio
{
    /// <summary>Put one in a scene to choose its music. Leave Track empty for silence.</summary>
    public class SceneMusic : MonoBehaviour
    {
        [SerializeField] MusicTrack track;
        [SerializeField] float fadeSeconds = 1.5f;

        void Start() => MusicManager.Play(track, fadeSeconds);
    }
}
