using Boruhan.Audio;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Boruhan.Map
{
    /// <summary>
    /// The look of a room made with the Oda Oluşturucu: which tiles fill the floor and the borders,
    /// lighting, background and music. Create your own with Assets > Create > Börühan > Oda Teması.
    /// </summary>
    [CreateAssetMenu(menuName = "Börühan/Oda Teması", fileName = "YeniTema")]
    public class RoomTheme : ScriptableObject
    {
        [Header("Döşemeler")]
        public TileBase floor;
        [Tooltip("Fills the area around the room (mountains, rock, walls). Should be solid.")]
        public TileBase borderFill;
        [Tooltip("Tall tiles along the inner edge of the border (peaks, stalagmites). Picked in turn.")]
        public TileBase[] borderEdge;
        [Min(1)] public int borderThickness = 5;

        [Header("Atmosfer")]
        public Color background = Color.black;
        public Color lightColor = Color.white;
        [Range(0f, 2f)] public float lightIntensity = 1f;
        public float cameraSize = 5.5f;
        public MusicTrack music;
    }
}
