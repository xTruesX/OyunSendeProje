using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Dialogue
{
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Who is speaking. Leave empty for narration.")]
        public DialogueSpeaker speaker;

        [TextArea(2, 6)]
        public string text;

        [Tooltip("Voice recording for this line. Plays when the line appears.")]
        public AudioClip voice;

        [Tooltip("Only used in cutscenes (timed subtitles): seconds this line stays on screen. 0 = automatic, based on length.")]
        [Min(0f)]
        public float duration;

        [Tooltip("Optional story flag set the moment this line appears (e.g. to make the stairway appear mid-dialogue).")]
        public string setFlag;
    }

    /// <summary>A list of lines played in order. Create with Assets > Create > Börühan > Diyalog.</summary>
    [CreateAssetMenu(menuName = "Börühan/Diyalog", fileName = "YeniDiyalog")]
    public class DialogueAsset : ScriptableObject
    {
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}
