using UnityEngine;

namespace Boruhan.Audio
{
    /// <summary>
    /// One song. Scenes point at this asset, so swapping the clip here changes the music everywhere it's used.
    /// Create with Assets > Create > Börühan > Müzik Parçası.
    /// </summary>
    [CreateAssetMenu(menuName = "Börühan/Müzik Parçası", fileName = "YeniMuzik")]
    public class MusicTrack : ScriptableObject
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
        public bool loop = true;
    }
}
