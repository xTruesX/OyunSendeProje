using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using UnityEngine;

namespace Boruhan.Story
{
    public enum PickupKind
    {
        [Tooltip("+max health, heals fully. Collected once per save.")] CanParcasi,
        [Tooltip("+10% damage. Collected once per save.")] KutParcasi,
        [Tooltip("Heals 40. Grows back when the area is re-entered.")] SifaOtu,
        [Tooltip("A story item (sword, boss reward…): sets Grant Flag and plays Pickup Dialogue. Collected once.")] OzelEsya,
    }

    /// <summary>Something Börühan picks up by walking over it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Pickup : MonoBehaviour
    {
        [SerializeField] PickupKind kind;
        [Tooltip("Unique within the game (e.g. kat1_vaha). Used to remember it was taken.")]
        [SerializeField] string id;
        [Tooltip("Shown when picked up. Empty = a default message.")]
        [TextArea] [SerializeField] string message;
        [SerializeField] float healAmount = 40f;

        [Header("Özel Eşya")]
        [Tooltip("Flag set when picked up (e.g. ayna_sicramasi). It also remembers that it was taken.")]
        [SerializeField] string grantFlag;
        [Tooltip("Played when picked up. Empty = Message.")]
        [SerializeField] DialogueAsset pickupDialogue;

        Vector3 baseScale;

        string Flag => kind switch
        {
            PickupKind.CanParcasi => "can_parcasi_" + id,
            PickupKind.KutParcasi => "kut_parcasi_" + id,
            PickupKind.OzelEsya => grantFlag,
            _ => null,
        };

        public bool IsCollected => !string.IsNullOrEmpty(Flag) && GameSession.HasFlag(Flag);

        void Start()
        {
            baseScale = transform.localScale;
            if (Flag != null && GameSession.HasFlag(Flag))
                Destroy(gameObject);
        }

        void Update() => transform.localScale = baseScale * (1f + 0.08f * Mathf.Sin(Time.time * 4f));

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        // Picked up as soon as no dialogue is showing, even if it appeared right under the player mid-dialogue.
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        bool collected;

        void TryCollect(Collider2D other)
        {
            if (collected || !other.CompareTag("Player") || DialogueRunner.IsBusy)
                return;
            var combat = other.GetComponentInParent<PlayerCombat>();
            var health = other.GetComponentInParent<Health>();
            if (combat == null || health == null || health.IsDead)
                return;

            collected = true;
            GameSession.SetFlag(Flag);
            Color color;
            string text;
            switch (kind)
            {
                case PickupKind.CanParcasi:
                    combat.ApplyUpgrades(true);
                    color = new Color(1f, 0.35f, 0.35f);
                    text = "Bir Can Parçası buldun! Canın arttı ve tamamen yenilendi.";
                    break;
                case PickupKind.KutParcasi:
                    combat.ApplyUpgrades(false);
                    color = new Color(1f, 0.65f, 0.2f);
                    text = "Bir Kut Parçası buldun! Vuruşların daha güçlü.";
                    break;
                case PickupKind.OzelEsya:
                    color = new Color(1f, 0.85f, 0.35f);
                    text = null;
                    if (pickupDialogue != null && string.IsNullOrEmpty(message))
                        DialogueRunner.Play(pickupDialogue);
                    break;
                default:
                    health.Heal(healAmount);
                    color = new Color(0.5f, 1f, 0.55f);
                    text = null;
                    break;
            }

            CombatFx.Burst(CombatFx.Circle, transform.position, 0.4f, 2.2f, new Color(color.r, color.g, color.b, 0.8f), 0.4f, 70);
            if (!string.IsNullOrEmpty(message))
                text = message;
            if (!string.IsNullOrEmpty(text))
                DialogueRunner.Play(OneLine(text));
            Destroy(gameObject);
        }

        static DialogueAsset OneLine(string text)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.lines.Add(new DialogueLine { text = text });
            return asset;
        }
    }
}
