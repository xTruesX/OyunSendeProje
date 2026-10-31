using Boruhan.Core;
using Boruhan.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Boruhan.Gameplay
{
    /// <summary>Top-down 8-way movement with a 4-way facing direction (Undertale style).</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] InputActionAsset actions;
        [SerializeField] Transform facingIndicator;

        Rigidbody2D body;
        InputAction moveAction;
        Vector2 moveInput;

        public bool InputEnabled { get; set; } = true;
        /// <summary>Scales walking speed, e.g. while swinging the sword.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public Vector2 MoveInput => moveInput;
        public bool IsSlowed => Time.time < slowUntil;

        Vector2 forcedVelocity;
        float forcedUntil;
        float slowMultiplier = 1f;
        float slowUntil;
        public Vector2 Facing { get; private set; } = Vector2.down;

        /// <summary>Last non-zero movement direction (8-way, normalized). Used by the talisman.</summary>
        public Vector2 LastMoveDirection { get; private set; } = Vector2.down;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            // Each player gets its own copy of the actions. With a shared asset, the previous scene's
            // player being destroyed after this one is enabled would switch movement off for both.
            actions = Instantiate(actions);
            moveAction = actions.FindAction("Player/Move", true);
        }

        void OnEnable() => moveAction.Enable();
        void OnDisable() => moveAction.Disable();
        void OnDestroy() => Destroy(actions);

        void Update()
        {
            bool canMove = InputEnabled && !DialogueRunner.IsBusy && !ScreenFader.IsBusy;
            moveInput = canMove ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                LastMoveDirection = moveInput.normalized;
                Facing = Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y)
                    ? new Vector2(Mathf.Sign(moveInput.x), 0f)
                    : new Vector2(0f, Mathf.Sign(moveInput.y));
            }

            if (facingIndicator != null)
                facingIndicator.localPosition = new Vector3(Facing.x * 0.3f, 0.5f + Facing.y * 0.4f, 0f);
        }

        void FixedUpdate()
        {
            if (Time.time < forcedUntil)
                body.linearVelocity = forcedVelocity;
            else
                body.linearVelocity = moveInput * moveSpeed * SpeedMultiplier * (IsSlowed ? slowMultiplier : 1f);
        }

        /// <summary>Moves at a fixed velocity for a moment, ignoring input (dodge, knockback, sword lunge).</summary>
        public void ForceVelocity(Vector2 velocity, float seconds)
        {
            forcedVelocity = velocity;
            forcedUntil = Time.time + seconds;
        }

        /// <summary>Slows walking (e.g. the Korku curse). The stronger slow wins if two overlap.</summary>
        public void ApplySlow(float multiplier, float seconds)
        {
            slowMultiplier = IsSlowed ? Mathf.Min(slowMultiplier, multiplier) : multiplier;
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }

        public void Teleport(Vector2 position)
        {
            // RoomManager may place the player before this object's Awake has run.
            if (body == null)
                body = GetComponent<Rigidbody2D>();
            body.position = position;
            body.linearVelocity = Vector2.zero;
            transform.position = position;
        }
    }
}
