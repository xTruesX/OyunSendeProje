using Boruhan.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Boruhan.UI
{
    public class SaveSlotButton : MonoBehaviour
    {
        [SerializeField] Button selectButton;
        [SerializeField] Text label;
        [SerializeField] Button deleteButton;
        [SerializeField] Text deleteLabel;

        int slot;
        bool confirmingDelete;

        public Button SelectButton => selectButton;

        public void Init(int slotIndex, MainMenuController menu)
        {
            slot = slotIndex;
            selectButton.onClick.AddListener(() => menu.OnSlotChosen(slot));
            deleteButton.onClick.AddListener(OnDelete);
        }

        public void Refresh()
        {
            confirmingDelete = false;
            deleteLabel.text = "Sil";

            bool exists = SaveSystem.Exists(slot);
            deleteButton.gameObject.SetActive(exists);

            string title = $"Kayıt {slot + 1}";
            if (!exists)
            {
                label.text = $"{title}\n<color=#8f8676>Boş — Yeni Oyun</color>";
                return;
            }

            var data = SaveSystem.Load(slot);
            label.text = data == null
                ? $"{title}\n<color=#c0504d>Kayıt dosyası okunamadı</color>"
                : $"{title}\n<color=#cfc4ad>{(data.floor > 0 ? $"{data.floor}. Kat · " : "")}{data.locationName} · {data.FormatPlayTime()} · {data.FormatLastSaved()}</color>";
        }

        void OnDelete()
        {
            if (!confirmingDelete)
            {
                confirmingDelete = true;
                deleteLabel.text = "Emin misin?";
                return;
            }

            SaveSystem.Delete(slot);
            Refresh();
        }
    }
}
