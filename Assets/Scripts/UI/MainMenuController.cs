using Boruhan.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Boruhan.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject slotPanel;
        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;
        [SerializeField] Button backButton;
        [Tooltip("Image shown next to the selected button. Empty = default circle.")]
        [SerializeField] Sprite selectionMarkerSprite;

        SaveSlotButton[] slots;

        void Awake()
        {
            Time.timeScale = 1f;

            slots = slotPanel.GetComponentsInChildren<SaveSlotButton>(true);
            for (int i = 0; i < slots.Length; i++)
                slots[i].Init(i, this);

            startButton.onClick.AddListener(ShowSlots);
            quitButton.onClick.AddListener(Quit);
            backButton.onClick.AddListener(ShowMain);

            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<SelectionMarker>() != null)
                    continue;

                var marker = button.gameObject.AddComponent<SelectionMarker>();
                if (selectionMarkerSprite != null)
                    marker.SetSprite(selectionMarkerSprite);
            }
        }

        // Done in Start so the EventSystem is guaranteed to be enabled before the first selection.
        void Start() => ShowMain();

        void Update()
        {
            if (slotPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ShowMain();
        }

        void ShowMain()
        {
            mainPanel.SetActive(true);
            slotPanel.SetActive(false);
            Select(startButton);
        }

        void ShowSlots()
        {
            foreach (var slot in slots)
                slot.Refresh();

            mainPanel.SetActive(false);
            slotPanel.SetActive(true);
            Select(slots.Length > 0 ? slots[0].SelectButton : backButton);
        }

        public void OnSlotChosen(int slot)
        {
            // A false return means the save file exists but couldn't be read; the slot label already says so.
            if (!GameSession.StartSlot(slot))
                slots[slot].Refresh();
        }

        static void Select(Button button)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
