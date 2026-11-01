using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boruhan.Animations;
using Boruhan.Audio;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Dialogue;
using Boruhan.Gameplay;
using Boruhan.Story;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.Events
{
    static class TextUtil
    {
        public static string Short(string s, int max = 40)
        {
            s = (s ?? "").Replace('\n', ' ');
            return s.Length > max ? s.Substring(0, max) + "…" : s;
        }
    }

    // ===================== Mesaj =====================

    [Serializable, CommandInfo("Mesaj/Metin Göster", "Konuşmacı ve metin. Her satır ayrı bir konuşma kutusu.")]
    public class ShowTextCommand : EventCommand
    {
        [Tooltip("Boş = anlatıcı")] public DialogueSpeaker speaker;
        [TextArea(2, 6)] public string text = "";
        [Tooltip("Voice recordings, one per text line, in order.")]
        public List<AudioClip> voices = new List<AudioClip>();

        public override IEnumerator Run(EventContext ctx)
        {
            bool done = false;
            DialogueRunner.PlayText(speaker, text, () => done = true, voices);
            while (!done)
                yield return null;
        }

        public override string Summary => $"Metin: {(speaker != null ? speaker.displayName + ": " : "")}{TextUtil.Short(text)}";
    }

    [Serializable, CommandInfo("Mesaj/Diyalog Oynat", "Hazır bir Diyalog dosyasını oynatır.")]
    public class ShowDialogueCommand : EventCommand
    {
        public DialogueAsset dialogue;

        public override IEnumerator Run(EventContext ctx)
        {
            bool done = false;
            DialogueRunner.Play(dialogue, () => done = true);
            while (!done)
                yield return null;
        }

        public override string Summary => $"Diyalog: {(dialogue != null ? dialogue.name : "(seçilmedi)")}";
    }

    [Serializable]
    public class ChoiceOption
    {
        public string text = "Seçenek";
        [SerializeReference] public List<EventCommand> commands = new List<EventCommand>();
    }

    [Serializable, CommandInfo("Mesaj/Seçenek Sun", "Oyuncuya soru sorar; her seçeneğin kendi komutları vardır.")]
    public class ShowChoicesCommand : EventCommand
    {
        public string question = "";
        public List<ChoiceOption> options = new List<ChoiceOption> { new ChoiceOption { text = "Evet" }, new ChoiceOption { text = "Hayır" } };

        public override IEnumerator Run(EventContext ctx)
        {
            int chosen = -1;
            DialogueRunner.Choose(question, options.Select(o => o.text).ToList(), i => chosen = i);
            while (chosen < 0)
                yield return null;
            if (chosen < options.Count)
                yield return EventInterpreter.Run(options[chosen].commands, ctx);
        }

        public override string Summary => $"Seçenek: {TextUtil.Short(question, 25)} [{string.Join(" / ", options.Select(o => o.text))}]";
    }

    // ===================== Akış =====================

    [Serializable, CommandInfo("Akış/Koşul (Eğer)", "Koşullar sağlanırsa 'O zaman', değilse 'Değilse' komutlarını çalıştırır.")]
    public class IfCommand : EventCommand
    {
        public List<EventCondition> conditions = new List<EventCondition>();
        [Tooltip("Açık: hepsi sağlanmalı. Kapalı: biri yeter.")]
        public bool requireAll = true;
        [SerializeReference] public List<EventCommand> then = new List<EventCommand>();
        [SerializeReference] public List<EventCommand> otherwise = new List<EventCommand>();

        public override IEnumerator Run(EventContext ctx)
        {
            bool ok = requireAll
                ? EventCondition.AllMet(conditions, ctx.Source)
                : conditions.Any(c => c != null && c.IsMet(ctx.Source));
            yield return EventInterpreter.Run(ok ? then : otherwise, ctx);
        }

        public override string Summary => $"Eğer {string.Join(requireAll ? " ve " : " veya ", conditions.Select(c => c.ToString()))}";
    }

    [Serializable, CommandInfo("Akış/Bekle", "Belirtilen süre kadar bekler.")]
    public class WaitCommand : EventCommand
    {
        public float seconds = 1f;
        public override IEnumerator Run(EventContext ctx) { yield return new WaitForSeconds(seconds); }
        public override string Summary => $"Bekle {seconds} sn";
    }

    [Serializable, CommandInfo("Akış/Ortak Olay Çağır", "Bir Ortak Olay dosyasının komutlarını çalıştırır.")]
    public class CallCommonEventCommand : EventCommand
    {
        public CommonEvent commonEvent;
        public override IEnumerator Run(EventContext ctx) { yield return EventInterpreter.Run(commonEvent != null ? commonEvent.commands : null, ctx); }
        public override string Summary => $"Ortak Olay: {(commonEvent != null ? commonEvent.name : "(seçilmedi)")}";
    }

    [Serializable, CommandInfo("Akış/Olayı Bitir", "Bu olayın kalan komutlarını çalıştırmaz.")]
    public class StopEventCommand : EventCommand
    {
        public override IEnumerator Run(EventContext ctx) { ctx.Stopped = true; yield break; }
        public override string Summary => "Olayı bitir";
    }

    // ===================== Durum =====================

    [Serializable, CommandInfo("Durum/Bayrak Ayarla", "Bir bayrağı açar ya da kapatır. 'self:x' bu olaya ait bayraktır.")]
    public class SetFlagCommand : EventCommand
    {
        public string flag = "self:A";
        public bool value = true;

        public override IEnumerator Run(EventContext ctx)
        {
            var key = ctx.Resolve(flag);
            if (value)
                GameSession.SetFlag(key);
            else
                GameSession.ClearFlag(key);
            yield break;
        }

        public override string Summary => $"Bayrak {flag} = {(value ? "açık" : "kapalı")}";
    }

    public enum VariableOperation { [InspectorName("Ata (=)")] Set, [InspectorName("Ekle (+)")] Add, [InspectorName("Çıkar (−)")] Subtract }

    [Serializable, CommandInfo("Durum/Değişken", "Bir sayı değişkenini değiştirir (ör. toplanan çiçekler).")]
    public class VariableCommand : EventCommand
    {
        public string variable = "sayac";
        public VariableOperation operation = VariableOperation.Add;
        public int value = 1;

        public override IEnumerator Run(EventContext ctx)
        {
            var key = ctx.Resolve(variable);
            int current = GameSession.GetVariable(key);
            GameSession.SetVariable(key, operation == VariableOperation.Set ? value : operation == VariableOperation.Add ? current + value : current - value);
            yield break;
        }

        public override string Summary => $"Değişken {variable} {(operation == VariableOperation.Set ? "=" : operation == VariableOperation.Add ? "+=" : "-=")} {value}";
    }

    public enum Power
    {
        [InspectorName("Kılıç")] Sword,
        [InspectorName("Kut Kıvılcımı")] KutSpark,
        [InspectorName("Ayna Sıçraması")] MirrorLeap,
        [InspectorName("Demir Eldiven")] IronGauntlet,
        [InspectorName("Zehir Bağışıklığı")] PoisonImmunity,
    }

    [Serializable, CommandInfo("Durum/Güç Ver", "Oyuncuya bir hikâye gücü verir (Ayna Sıçraması, Demir Eldiven…).")]
    public class GrantPowerCommand : EventCommand
    {
        public Power power;

        public static string FlagOf(Power power) => power switch
        {
            Power.Sword => StoryFlags.Sword,
            Power.KutSpark => StoryFlags.KutSpark,
            Power.MirrorLeap => StoryFlags.MirrorLeap,
            Power.IronGauntlet => StoryFlags.IronGauntlet,
            _ => StoryFlags.PoisonImmunity,
        };

        public override IEnumerator Run(EventContext ctx) { GameSession.SetFlag(FlagOf(power)); yield break; }
        public override string Summary => $"Güç ver: {power}";
    }

    [Serializable, CommandInfo("Durum/Oyunu Kaydet", "Oyunu şu anki konumla kaydeder.")]
    public class SaveGameCommand : EventCommand
    {
        public override IEnumerator Run(EventContext ctx)
        {
            // No saving in the middle of a boss fight.
            if (RoomManager.Instance != null && !BossFight.InProgress)
                RoomManager.Instance.SaveGame();
            yield break;
        }
        public override string Summary => "Oyunu kaydet (boss savaşında atlanır)";
    }

    // ===================== Oyuncu =====================

    [Serializable, CommandInfo("Oyuncu/Işınla", "Başka sahneye ya da bu sahnede bir noktaya götürür.")]
    public class TeleportCommand : EventCommand
    {
        [Tooltip("Boş = bu sahne")] public string scene = "";
        [Tooltip("Hedef sahnedeki Varış Noktası (SpawnPoint) id'si")] public string spawnId = "";
        [Tooltip("Aynı sahnede ve varış noktası boşsa: bu koordinata")] public Vector2 position;

        public override IEnumerator Run(EventContext ctx)
        {
            if (!string.IsNullOrEmpty(scene) && scene != SceneManager.GetActiveScene().name)
            {
                ctx.Stopped = true;
                GameSession.TravelTo(scene, spawnId);
                yield break;
            }

            var target = position;
            if (!string.IsNullOrEmpty(spawnId))
            {
                var spawn = UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude).FirstOrDefault(s => s.id == spawnId);
                if (spawn != null)
                    target = spawn.transform.position;
            }
            ctx.Player?.Teleport(target);
        }

        public override string Summary => string.IsNullOrEmpty(scene) ? $"Işınla: {(string.IsNullOrEmpty(spawnId) ? position.ToString() : spawnId)}" : $"Işınla: {scene} ({spawnId})";
    }

    [Serializable, CommandInfo("Oyuncu/İyileştir", "Canı doldurur.")]
    public class HealCommand : EventCommand
    {
        public bool full = true;
        public float amount = 30f;
        public override IEnumerator Run(EventContext ctx)
        {
            var h = ctx.PlayerHealth;
            if (h != null)
            {
                if (full) h.HealFull(); else h.Heal(amount);
            }
            yield break;
        }
        public override string Summary => full ? "İyileştir (tam)" : $"İyileştir +{amount}";
    }

    [Serializable, CommandInfo("Oyuncu/Hasar Ver", "Oyuncuya hasar verir (tuzak vb.).")]
    public class DamageCommand : EventCommand
    {
        public float amount = 10f;
        public override IEnumerator Run(EventContext ctx)
        {
            var h = ctx.PlayerHealth;
            h?.Damage(new DamageInfo { amount = amount, source = h.transform.position });
            yield break;
        }
        public override string Summary => $"Hasar {amount}";
    }

    [Serializable, CommandInfo("Oyuncu/Eşya Ver", "Can Parçası, Kut Parçası ya da Şifa Otu verir.")]
    public class GiveItemCommand : EventCommand
    {
        public PickupKind item;
        [Tooltip("Gösterilecek mesaj. Boş = varsayılan.")] public string message = "";

        public override IEnumerator Run(EventContext ctx)
        {
            var player = ctx.Player;
            if (player == null)
                yield break;
            var combat = player.GetComponent<PlayerCombat>();
            var health = player.GetComponent<Health>();
            string text = message;
            switch (item)
            {
                case PickupKind.CanParcasi:
                    GameSession.SetFlag("can_parcasi_" + Guid.NewGuid().ToString("N"));
                    combat?.ApplyUpgrades(true);
                    if (string.IsNullOrEmpty(text)) text = "Bir Can Parçası aldın! Canın arttı.";
                    break;
                case PickupKind.KutParcasi:
                    GameSession.SetFlag("kut_parcasi_" + Guid.NewGuid().ToString("N"));
                    combat?.ApplyUpgrades(false);
                    if (string.IsNullOrEmpty(text)) text = "Bir Kut Parçası aldın! Vuruşların güçlendi.";
                    break;
                case PickupKind.SifaOtu:
                    health?.Heal(40f);
                    if (string.IsNullOrEmpty(text)) text = "Şifa Otu yedin.";
                    break;
                default:
                    // Story items carry their own flag: use "Güç Ver" or "Bayrak Ayarla" for those.
                    yield break;
            }
            bool done = false;
            DialogueRunner.PlayText(null, text, () => done = true);
            while (!done)
                yield return null;
        }

        public override string Summary => $"Eşya ver: {item}";
    }

    // ===================== Sahne =====================

    [Serializable, CommandInfo("Sahne/Müzik Çal")]
    public class PlayMusicCommand : EventCommand
    {
        public MusicTrack track;
        public float fadeSeconds = 1.5f;
        public override IEnumerator Run(EventContext ctx) { MusicManager.Play(track, fadeSeconds); yield break; }
        public override string Summary => $"Müzik: {(track != null ? track.name : "(sessiz)")}";
    }

    [Serializable, CommandInfo("Sahne/Müziği Durdur")]
    public class StopMusicCommand : EventCommand
    {
        public float fadeSeconds = 1.5f;
        public override IEnumerator Run(EventContext ctx) { MusicManager.Stop(fadeSeconds); yield break; }
        public override string Summary => "Müziği durdur";
    }

    [Serializable, CommandInfo("Sahne/Ses Çal", "Tek seferlik bir ses efekti.")]
    public class PlaySoundCommand : EventCommand
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public override IEnumerator Run(EventContext ctx)
        {
            if (clip != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
            yield break;
        }
        public override string Summary => $"Ses: {(clip != null ? clip.name : "(seçilmedi)")}";
    }

    [Serializable, CommandInfo("Sahne/Ekranı Salla")]
    public class ShakeCameraCommand : EventCommand
    {
        public float seconds = 0.6f;
        public override IEnumerator Run(EventContext ctx)
        {
            var cam = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null)
                cam.Shake(seconds);
            yield break;
        }
        public override string Summary => $"Ekranı salla {seconds} sn";
    }

    [Serializable, CommandInfo("Sahne/Nesneyi Göster-Gizle", "Sahnedeki bir nesneyi açar ya da kapatır (köprü, kapı, duvar…).")]
    public class SetActiveCommand : EventCommand
    {
        public GameObject target;
        public bool active = true;
        public override IEnumerator Run(EventContext ctx) { if (target != null) target.SetActive(active); yield break; }
        public override string Summary => $"{(target != null ? target.name : "(nesne)")} {(active ? "göster" : "gizle")}";
    }

    [Serializable, CommandInfo("Sahne/Düşman Çıkar", "Olayın yanında bir düşman belirir. İsteğe bağlı: yenilene kadar bekle.")]
    public class SpawnEnemyCommand : EventCommand
    {
        public EnemyDefinition enemy;
        [Min(1)] public int count = 1;
        [Tooltip("Olayın konumuna göre")] public Vector2 offset = new Vector2(0f, 2f);
        public bool waitUntilDefeated;

        public override IEnumerator Run(EventContext ctx)
        {
            if (enemy == null)
                yield break;
            var origin = ctx.Source != null ? (Vector2)ctx.Source.transform.position : Vector2.zero;
            var spawned = new List<EnemyController>();
            for (int i = 0; i < count; i++)
            {
                var e = EnemyController.Spawn(enemy, origin + offset + new Vector2((i - (count - 1) / 2f) * 1.2f, 0f));
                e.ForceAggro();
                spawned.Add(e);
            }
            if (waitUntilDefeated)
            {
                while (spawned.Any(e => e != null && !e.Health.IsDead))
                    yield return null;
            }
        }

        public override string Summary => $"Düşman çıkar: {(enemy != null ? enemy.displayName : "(seçilmedi)")} x{count}{(waitUntilDefeated ? " (yenilene kadar bekle)" : "")}";
    }

    // ===================== Olay =====================

    [Serializable, CommandInfo("Olay/Olayı Hareket Ettir", "Olayı (ör. bir NPC'yi) belirtilen kadar yürütür.")]
    public class MoveEventCommand : EventCommand
    {
        public Vector2 offset = new Vector2(0f, 2f);
        public float speed = 2f;

        public override IEnumerator Run(EventContext ctx)
        {
            if (ctx.Source == null)
                yield break;
            var t = ctx.Source.transform;
            Vector2 target = (Vector2)t.position + offset;
            while (Vector2.Distance(t.position, target) > 0.02f)
            {
                t.position = Vector2.MoveTowards(t.position, target, speed * Time.deltaTime);
                yield return null;
            }
        }

        public override string Summary => $"Olayı yürüt {offset}";
    }

    [Serializable, CommandInfo("Olay/Animasyon Oynat", "Bir karakterin Animasyon Setindeki bir animasyonu bir kez oynatır (ara sahneler için). Hedef boş = bu olay; Oyuncu = Börühan.")]
    public class PlayAnimationCommand : EventCommand
    {
        [Tooltip("Boş = bu olay")] public GameObject target;
        [Tooltip("Hedef yerine Börühan")] public bool player;
        public AnimState state = AnimState.Custom;
        [Tooltip("Sadece Özel için: animasyonun adı")] public string customName = "";
        [Tooltip("En az bu kadar sürsün (saniye). Döngülü animasyonlar için gerekli.")] [Min(0f)] public float seconds;
        [Tooltip("Bitene kadar sonraki komuta geçme")] public bool wait = true;

        public override IEnumerator Run(EventContext ctx)
        {
            var go = player ? GameObject.FindWithTag("Player") : target != null ? target : ctx.Source != null ? ctx.Source.gameObject : null;
            var animator = go != null ? go.GetComponentInChildren<CharacterAnimator>() : null;
            if (animator == null || !animator.PlayAction(state, seconds, customName))
                yield break;
            while (wait && animator != null && animator.IsPlayingAction)
                yield return null;
        }

        public override string Summary
        {
            get
            {
                string who = player ? "Börühan" : target != null ? target.name : "bu olay";
                string what = state == AnimState.Custom ? $"“{customName}”" : state.Turkish();
                return $"Animasyon: {who} → {what}";
            }
        }
    }

    [Serializable, CommandInfo("Olay/Olayı Sil (Geçici)", "Olay, sahneye tekrar girilene kadar kaybolur.")]
    public class EraseEventCommand : EventCommand
    {
        public override IEnumerator Run(EventContext ctx) { ctx.Source?.Erase(); yield break; }
        public override string Summary => "Olayı sil (geçici)";
    }
}
