using UnityEngine;

namespace Boruhan.Dialogue
{
    /// <summary>A character who can speak in dialogue. Change the name, portrait or color here and every line updates.</summary>
    [CreateAssetMenu(menuName = "Börühan/Diyalog Konuşmacısı", fileName = "YeniKonusmaci")]
    public class DialogueSpeaker : ScriptableObject
    {
        public string displayName = "İsim";
        public Sprite portrait;
        public Color nameColor = new Color32(212, 168, 75, 255);
    }
}
