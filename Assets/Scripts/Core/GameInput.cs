using UnityEngine;
using UnityEngine.InputSystem;

namespace Boruhan.Core
{
    /// <summary>Shared buttons for interacting / advancing dialogue and for skipping.</summary>
    public static class GameInput
    {
        static InputAction submit;

        // The project enters Play Mode without a domain reload, so statics survive between sessions while
        // the Input System tears actions down on exit. Start fresh every time.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            submit?.Dispose();
            submit = null;
        }

        /// <summary>E, Enter, Space or gamepad A, pressed this frame.</summary>
        public static bool SubmitPressed
        {
            get
            {
                if (submit == null)
                {
                    submit = new InputAction("Onayla", InputActionType.Button);
                    submit.AddBinding("<Keyboard>/e");
                    submit.AddBinding("<Keyboard>/enter");
                    submit.AddBinding("<Keyboard>/numpadEnter");
                    submit.AddBinding("<Keyboard>/space");
                    submit.AddBinding("<Gamepad>/buttonSouth");
                }
                if (!submit.enabled)
                    submit.Enable();
                return submit.WasPressedThisFrame();
            }
        }

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
    }
}
