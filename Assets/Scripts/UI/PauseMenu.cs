using Boruhan.Core;
using Boruhan.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Boruhan.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button saveButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] Text statusText;
        [SerializeField] PlayerController player;

        bool paused;

        void Awake()
        {
            // The pause menu lives on the HUD prefab, so the scene's player is looked up rather than assigned.
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();

            resumeButton.onClick.AddListener(() => SetPaused(false));
            saveButton.onClick.AddListener(OnSave);
            mainMenuButton.onClick.AddListener(OnMainMenu);
            panel.SetActive(false);
        }

        void Update()
        {
            bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                           || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (pressed && !DeathScreen.IsOpen)
                SetPaused(!paused);
        }

        void SetPaused(bool value)
        {
            paused = value;
            panel.SetActive(value);
            Time.timeScale = value ? 0f : 1f;
            player.InputEnabled = !value;
            statusText.text = value ? Progress() : "";

            if (value && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        static string Progress()
        {
            var text = $"Can Parçası: {GameSession.CountFlags("can_parcasi_")}   ·   Kut Parçası: {GameSession.CountFlags("kut_parcasi_")}   ·   Yazıt: {GameSession.CountFlags("yazit_")}";
            var gifts = new System.Collections.Generic.List<string>();
            if (GameSession.HasFlag(StoryFlags.MirrorLeap)) gifts.Add("Ayna Sıçraması");
            if (GameSession.HasFlag(StoryFlags.IronGauntlet)) gifts.Add("Demir Eldiven");
            if (GameSession.HasFlag(StoryFlags.PoisonImmunity)) gifts.Add("Zehir Bağışıklığı");
            return gifts.Count > 0 ? text + "\nKazanılan güçler: " + string.Join(", ", gifts) : text;
        }

        void OnSave()
        {
            if (!GameSession.CanSave)
                statusText.text = "Test modunda kayıt yapılmaz.";
            else if (Combat.BossFight.InProgress)
                statusText.text = "Boss savaşı sürerken kayıt yapılamaz.";
            else
                statusText.text = RoomManager.Instance.SaveGame() ? "Oyun kaydedildi." : "Kayıt başarısız!";
        }

        void OnMainMenu()
        {
            RoomManager.Instance.SaveGame();
            Time.timeScale = 1f;
            GameSession.End();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        void OnDestroy() => Time.timeScale = 1f;
    }
}
