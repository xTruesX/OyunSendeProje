using System;
using System.Collections;
using System.Collections.Generic;
using Boruhan.Combat;
using Boruhan.Core;
using Boruhan.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boruhan.Events
{
    public enum ConditionKind
    {
        [InspectorName("Bayrak açık")] FlagSet,
        [InspectorName("Bayrak kapalı")] FlagNotSet,
        [InspectorName("Değişken ≥")] VariableAtLeast,
        [InspectorName("Değişken <")] VariableBelow,
        [InspectorName("Değişken =")] VariableEquals,
        [InspectorName("Can Parçası ≥")] HealthShardsAtLeast,
        [InspectorName("Kut Parçası ≥")] KutShardsAtLeast,
        [InspectorName("Boss savaşı sürüyor")] BossFightActive,
    }

    /// <summary>
    /// One check used by event pages and "Koşul" commands. Flag names starting with "self:" belong to
    /// the event itself (like RPG Maker's self switches), e.g. "self:acildi".
    /// </summary>
    [Serializable]
    public class EventCondition
    {
        public ConditionKind kind;
        [Tooltip("Flag or variable name. 'self:x' = this event's own flag.")]
        public string key;
        public int value;

        public bool IsMet(GameEvent source)
        {
            var k = EventContext.Resolve(key, source);
            switch (kind)
            {
                case ConditionKind.FlagSet: return GameSession.HasFlag(k);
                case ConditionKind.FlagNotSet: return !GameSession.HasFlag(k);
                case ConditionKind.VariableAtLeast: return GameSession.GetVariable(k) >= value;
                case ConditionKind.VariableBelow: return GameSession.GetVariable(k) < value;
                case ConditionKind.VariableEquals: return GameSession.GetVariable(k) == value;
                case ConditionKind.HealthShardsAtLeast: return GameSession.CountFlags("can_parcasi_") >= value;
                case ConditionKind.KutShardsAtLeast: return GameSession.CountFlags("kut_parcasi_") >= value;
                case ConditionKind.BossFightActive: return BossFight.InProgress;
                default: return true;
            }
        }

        public static bool AllMet(List<EventCondition> conditions, GameEvent source)
        {
            if (conditions == null)
                return true;
            foreach (var c in conditions)
            {
                if (c != null && !c.IsMet(source))
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            switch (kind)
            {
                case ConditionKind.FlagSet: return $"{key} açık";
                case ConditionKind.FlagNotSet: return $"{key} kapalı";
                case ConditionKind.VariableAtLeast: return $"{key} ≥ {value}";
                case ConditionKind.VariableBelow: return $"{key} < {value}";
                case ConditionKind.VariableEquals: return $"{key} = {value}";
                case ConditionKind.HealthShardsAtLeast: return $"Can Parçası ≥ {value}";
                case ConditionKind.BossFightActive: return "Boss savaşı sürüyor";
                default: return $"Kut Parçası ≥ {value}";
            }
        }
    }

    /// <summary>What a running event knows: which event started it, and whether it was told to stop.</summary>
    public class EventContext
    {
        public GameEvent Source;
        public bool Stopped;

        public PlayerController Player
        {
            get
            {
                var go = GameObject.FindWithTag("Player");
                return go != null ? go.GetComponent<PlayerController>() : null;
            }
        }

        public Health PlayerHealth
        {
            get
            {
                var go = GameObject.FindWithTag("Player");
                return go != null ? go.GetComponent<Health>() : null;
            }
        }

        public string Resolve(string key) => Resolve(key, Source);

        /// <summary>"self:x" → "&lt;scene&gt;/&lt;event id&gt;/x"; anything else is used as is.</summary>
        public static string Resolve(string key, GameEvent source)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith("self:") || source == null)
                return key;
            return $"{SceneManager.GetActiveScene().name}/{source.Id}/{key.Substring(5).Trim()}";
        }
    }

    /// <summary>Menu path and help text for a command in the editor's "+ Komut Ekle" menu.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandInfoAttribute : Attribute
    {
        public readonly string Path;
        public readonly string Help;

        public CommandInfoAttribute(string path, string help = "")
        {
            Path = path;
            Help = help;
        }
    }

    /// <summary>
    /// One step of an event. To add a new kind of command: make a [Serializable] class that inherits
    /// from this, give it a [CommandInfo] and write Run(). It appears in the editor menu automatically.
    /// </summary>
    [Serializable]
    public abstract class EventCommand
    {
        public abstract IEnumerator Run(EventContext ctx);

        /// <summary>One-line description shown in the editor list.</summary>
        public virtual string Summary => GetType().Name;
    }

    public static class EventInterpreter
    {
        public static IEnumerator Run(List<EventCommand> commands, EventContext ctx)
        {
            if (commands == null)
                yield break;
            foreach (var command in commands)
            {
                if (ctx.Stopped)
                    yield break;
                if (command != null)
                    yield return command.Run(ctx);
            }
        }
    }
}
