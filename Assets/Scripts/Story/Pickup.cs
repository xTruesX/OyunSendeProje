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

        Vector3 baseScale;

        string Flag => kind switch
        {
            PickupKind.CanParcasi => "can_parcasi_" + id,
            PickupKind.KutParcasi => "kut_parcasi_" + id,
            _ => null,
        };

        void Start()
        {
            baseScale = transform.localScale;
            if (Flag != null && GameSession.HasFlag(Flag))
                Destroy(gameObject);
        }

        void Update() => transform.localScale = baseScale * (1f + 0.08f * Mathf.Sin(Time.time * 4f));

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;
            var combat = other.GetComponentInParent<PlayerCombat>();
            var health = other.GetComponentInParent<Health>();
            if (combat == null || health == null || health.IsDead)
                return;

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
