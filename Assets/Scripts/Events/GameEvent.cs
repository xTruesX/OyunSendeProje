using System;
using System.Collections;
using System.Collections.Generic;
using Boruhan.Animations;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using Boruhan.Story;
using UnityEngine;

namespace Boruhan.Events
{
    public enum EventTrigger
    {
        [InspectorName("Etkileşim (E tuşu)")] Interact,
        [InspectorName("Dokunma (üstüne yürü)")] Touch,
        [InspectorName("Otomatik (sayfa açılınca bir kez)")] Autorun,
        [InspectorName("Paralel (sürekli tekrar)")] Parallel,
    }

    public enum EventMovement
    {
        [InspectorName("Sabit")] Still,
        [InspectorName("Rastgele dolaş")] Wander,
        [InspectorName("Oyuncuya yaklaş")] ApproachPlayer,
    }

    /// <summary>One page of an event. The last page whose conditions are all met is the active one.</summary>
    [Serializable]
    public class EventPage
    {
        [Tooltip("Sadece sizin için not")] public string note = "";
        public List<EventCondition> conditions = new List<EventCondition>();
        public EventTrigger trigger = EventTrigger.Interact;

        [Header("Görünüm")]
        [Tooltip("Boş = görünmez")] public Sprite sprite;
        [Tooltip("İsteğe bağlı: yürüme, bekleme… kareleri. Boş = yukarıdaki sprite, hareketsiz.")]
        public AnimationSet animations;
        public Color tint = Color.white;
        [Tooltip("Açık: üstünden geçilemez")] public bool solid = true;
        public Vector2 size = Vector2.one;
        [Tooltip("[E] … olarak görünen yazı")] public string prompt = "Bak";

        [Header("Hareket")]
        public EventMovement movement = EventMovement.Still;
        public float moveSpeed = 1.5f;
        public float wanderRadius = 2f;

        [Tooltip("Olay çalışırken oyuncu yürüyemez")] public bool freezePlayer = true;

        [SerializeReference] public List<EventCommand> commands = new List<EventCommand>();
    }

    /// <summary>
    /// An RPG Maker style event: NPCs, chests, signs, doors, cutscene triggers… all built from pages of
    /// commands in the Inspector, without code.
    /// </summary>
    public class GameEvent : MonoBehaviour, IInteractable
    {
        [SerializeField] string id;
        [SerializeField] List<EventPage> pages = new List<EventPage> { new EventPage() };

        SpriteRenderer spriteRenderer;
        CharacterAnimator animator;
        BoxCollider2D box;
        EventPage active;
        int activeIndex = -1;
        bool running;
        bool erased;
        Vector2 home;
        Vector2 wanderTarget;
        float nextWander;

        public string Id => id;
        public List<EventPage> Pages => pages;
        public int ActivePageIndex => activeIndex;

        public string Prompt => active != null ? active.prompt : "";
        public bool CanInteract => isActiveAndEnabled && !erased && !running && active != null && active.trigger == EventTrigger.Interact;

        void Awake()
        {
            EnsureParts();
            home = transform.position;
            wanderTarget = home;
        }

        void OnEnable()
        {
            GameSession.FlagChanged += OnStateChanged;
            GameSession.VariableChanged += OnStateChanged;
        }

        void OnDisable()
        {
            GameSession.FlagChanged -= OnStateChanged;
            GameSession.VariableChanged -= OnStateChanged;
        }

        void Start() => Refresh();

        void OnStateChanged(string _) => Refresh();

        /// <summary>Picks the active page from the current flags/variables and applies its look.</summary>
        public void Refresh()
        {
            if (erased)
                return;
            int index = -1;
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                if (EventCondition.AllMet(pages[i].conditions, this))
                {
                    index = i;
                    break;
                }
            }
            bool changed = index != activeIndex;
            activeIndex = index;
            active = index >= 0 ? pages[index] : null;
            Apply(active);

            if (changed && active != null && !running && (active.trigger == EventTrigger.Autorun || active.trigger == EventTrigger.Parallel))
                StartCoroutine(Execute(active));
        }

        void Apply(EventPage page)
        {
            EnsureParts();
            spriteRenderer.enabled = page != null && (page.sprite != null || page.animations != null);
            if (page == null)
            {
                box.enabled = false;
                return;
            }
            spriteRenderer.sprite = page.sprite;
            if (animator == null)
                animator = spriteRenderer.GetComponent<CharacterAnimator>();
            if (animator == null && page.animations != null && Application.isPlaying)
                animator = spriteRenderer.gameObject.AddComponent<CharacterAnimator>();
            if (animator != null)
                animator.Setup(page.animations, page.sprite);
            spriteRenderer.color = page.tint;
            box.enabled = true;
            box.isTrigger = !page.solid;
            box.size = page.size;
            box.offset = new Vector2(0f, page.size.y / 2f);
        }

        void EnsureParts()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
                if (spriteRenderer == null)
                {
                    var go = new GameObject("Gorsel");
                    go.transform.SetParent(transform, false);
                    spriteRenderer = go.AddComponent<SpriteRenderer>();
                    spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                }
            }
            if (box == null)
            {
                box = GetComponent<BoxCollider2D>();
                if (box == null)
                    box = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        public void Interact()
        {
            if (TryGetComponent<SavePoint>(out var savePoint) && CanInteract && savePoint.TryBlock())
                return;
            if (CanInteract)
                StartCoroutine(Execute(active));
        }

        void OnTriggerEnter2D(Collider2D other) => Touched(other);
        void OnCollisionEnter2D(Collision2D collision) => Touched(collision.collider);

        void Touched(Collider2D other)
        {
            if (active == null || active.trigger != EventTrigger.Touch || running || erased || !other.CompareTag("Player"))
                return;
            if (TryGetComponent<SavePoint>(out var savePoint) && savePoint.TryBlock())
                return;
            StartCoroutine(Execute(active));
        }

        IEnumerator Execute(EventPage page)
        {
            if (running)
                yield break;
            running = true;
            do
            {
                while (DialogueRunner.IsBusy || ScreenFader.IsBusy)
                    yield return null;

                var ctx = new EventContext { Source = this };
                var player = ctx.Player;
                bool freeze = page.freezePlayer && page.trigger != EventTrigger.Parallel && player != null;
                if (freeze)
                    player.InputEnabled = false;

                yield return EventInterpreter.Run(page.commands, ctx);

                if (freeze && player != null)
                    player.InputEnabled = true;
                if (page.trigger == EventTrigger.Parallel)
                    yield return null;
            }
            while (page.trigger == EventTrigger.Parallel && page == active && !erased && isActiveAndEnabled);
            running = false;
            Refresh();
        }

        /// <summary>Hides the event until the scene is entered again.</summary>
        public void Erase()
        {
            erased = true;
            EnsureParts();
            spriteRenderer.enabled = false;
            box.enabled = false;
        }

        void Update()
        {
            if (active == null || running || erased || active.movement == EventMovement.Still)
                return;

            Vector2 pos = transform.position;
            Vector2 target = wanderTarget;
            if (active.movement == EventMovement.Wander)
            {
                if (Time.time > nextWander)
                {
                    wanderTarget = home + UnityEngine.Random.insideUnitCircle * active.wanderRadius;
                    nextWander = Time.time + UnityEngine.Random.Range(2f, 4f);
                }
            }
            else
            {
                var player = GameObject.FindWithTag("Player");
                if (player == null || Vector2.Distance(player.transform.position, pos) > 6f || Vector2.Distance(player.transform.position, pos) < 1.2f)
                    return;
                target = player.transform.position;
            }
            transform.position = Vector2.MoveTowards(pos, target, active.moveSpeed * Time.deltaTime);
            if (animator == null && spriteRenderer != null && Mathf.Abs(target.x - pos.x) > 0.05f)
                spriteRenderer.flipX = target.x < pos.x;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = NewId();
            // Preview the first page in the editor (after OnValidate, Unity doesn't allow it during).
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && !Application.isPlaying && pages.Count > 0)
                    Apply(pages[0]);
            };
        }

        public void RegenerateId() => id = NewId();

        void OnDrawGizmos()
        {
            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { normal = { textColor = new Color(0.4f, 0.8f, 1f) } };
            var trigger = pages.Count > 0 ? pages[Mathf.Max(0, activeIndex)].trigger.ToString() : "";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.6f, $"Olay: {name} ({pages.Count} sayfa, {trigger})", style);
            if (pages.Count > 0 && pages[0].sprite == null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.3f);
                var s = pages[0].size;
                Gizmos.DrawCube(transform.position + new Vector3(0f, s.y / 2f, 0f), s);
            }
        }
#endif

        static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
