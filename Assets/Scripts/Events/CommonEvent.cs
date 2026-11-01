using System.Collections.Generic;
using UnityEngine;

namespace Boruhan.Events
{
    /// <summary>
    /// A reusable list of commands (Ortak Olay), e.g. "Kut Ocağında dinlen" or "Tüccar konuşması".
    /// Call it from any event with "Ortak Olay Çağır". Create with Assets > Create > Börühan > Ortak Olay.
    /// </summary>
    [CreateAssetMenu(menuName = "Börühan/Ortak Olay", fileName = "YeniOrtakOlay")]
    public class CommonEvent : ScriptableObject
    {
        [TextArea] public string note;
        [SerializeReference] public List<EventCommand> commands = new List<EventCommand>();
    }
}
