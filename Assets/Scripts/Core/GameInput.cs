using UnityEngine;
using UnityEngine.InputSystem;

namespace Boruhan.Core
{
    /// <summary>Shared buttons: interacting / advancing dialogue, skipping, and combat.</summary>
    public static class GameInput
    {
        static InputAction submit;
        static InputAction lightAttack;
        static InputAction heavyAttack;
        static InputAction dodge;
        static InputAction parry;

        // The project enters Play Mode without a domain reload, so statics survive between sessions while
        // the Input System tears actions down on exit. Start fresh every time.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            foreach (var action in new[] { submit, lightAttack, heavyAttack, dodge, parry })
                action?.Dispose();
            submit = lightAttack = heavyAttack = dodge = parry = null;
        }

        /// <summary>E, Enter, Space or gamepad A, pressed this frame.</summary>
        public static bool SubmitPressed =>
            Pressed(ref submit, "Onayla", "<Keyboard>/e", "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Gamepad>/buttonSouth");

        /// <summary>Hafif saldırı: J, left click or gamepad X.</summary>
        public static bool LightAttackPressed =>
            Pressed(ref lightAttack, "HafifSaldiri", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest");

        /// <summary>Ağır saldırı: K, right click or gamepad Y.</summary>
        public static bool HeavyAttackPressed =>
            Pressed(ref heavyAttack, "AgirSaldiri", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/buttonNorth");

        /// <summary>Kaçış: Left Shift, L or gamepad B.</summary>
        public static bool DodgePressed =>
            Pressed(ref dodge, "Kacis", "<Keyboard>/leftShift", "<Keyboard>/l", "<Gamepad>/buttonEast");

        /// <summary>Savuşturma: Q or gamepad left bumper.</summary>
        public static bool ParryPressed =>
            Pressed(ref parry, "Savusturma", "<Keyboard>/q", "<Gamepad>/leftShoulder");

        /// <summary>Esc or gamepad Start, pressed this frame.</summary>
        public static bool CancelPressed =>
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        /// <summary>Skips a cutscene: Enter, Space, Esc, left click, or gamepad A / Start.</summary>
        public static bool SkipPressed
        {
            get
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
                                         || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                    return true;

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    return true;

                var gamepad = Gamepad.current;
                return gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame);
            }
        }

        static bool Pressed(ref InputAction action, string name, params string[] bindings)
        {
            if (action == null)
            {
                action = new InputAction(name, InputActionType.Button);
                foreach (var binding in bindings)
                    action.AddBinding(binding);
            }
            if (!action.enabled)
                action.Enable();
            return action.WasPressedThisFrame();
        }
    }
}
