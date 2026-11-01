using Boruhan.Animations;
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
        CharacterAnimator animator;
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

        float faceUntil;
        bool usingMouse = true;

        /// <summary>
        /// Where attacks go: towards the mouse pointer, or the walking direction when a gamepad was used
        /// last (or there is no mouse).
        /// </summary>
        public Vector2 AimDirection
        {
            get
            {
                var mouse = Mouse.current;
                var cam = Camera.main;
                if (!usingMouse || mouse == null || cam == null)
                    return LastMoveDirection;
                Vector2 pointer = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                var dir = pointer - ((Vector2)transform.position + Vector2.up * 0.5f);
                return dir.sqrMagnitude > 0.0001f ? dir.normalized : LastMoveDirection;
            }
        }

        /// <summary>Looks in a direction for a while (e.g. towards an attack), even while walking another way.</summary>
        public void FaceTowards(Vector2 direction, float seconds)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;
            Facing = FourWay(direction);
            faceUntil = Time.time + seconds;
            if (animator != null)
                animator.Facing = Facing;
        }

        static Vector2 FourWay(Vector2 v) => Mathf.Abs(v.x) > Mathf.Abs(v.y)
            ? new Vector2(Mathf.Sign(v.x), 0f)
            : new Vector2(0f, Mathf.Sign(v.y));

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<CharacterAnimator>();

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

            UpdateAimDevice();
            if (moveInput.sqrMagnitude > 0.01f)
            {
                LastMoveDirection = moveInput.normalized;
                if (Time.time >= faceUntil)
                    Facing = FourWay(moveInput);
            }

            if (animator != null)
                animator.Facing = Facing;
            if (facingIndicator != null)
                facingIndicator.localPosition = new Vector3(Facing.x * 0.3f, 0.5f + Facing.y * 0.4f, 0f);
        }

        // Moving or clicking the mouse aims with it; touching the gamepad aims where you walk.
        void UpdateAimDevice()
        {
            var mouse = Mouse.current;
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.5f || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                usingMouse = true;
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.2f || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame))
                usingMouse = false;
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
