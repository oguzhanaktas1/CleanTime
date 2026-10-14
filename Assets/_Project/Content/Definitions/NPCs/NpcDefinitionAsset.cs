using System;
using System.Linq;
using UnityEngine;
using CleanTime.NPC.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/NPC Definition", fileName = "SO_Npc_")]
    public sealed class NpcDefinitionAsset : ScriptableObject
    {
        [Serializable]
        public struct ScheduleEntry
        {
            public DayPhase phase;
            public string nodeId;
        }

        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private NpcRole role = NpcRole.Townsperson;
        [SerializeField] private string defaultNodeId = "home";
        [SerializeField] private ScheduleEntry[] schedule = Array.Empty<ScheduleEntry>();

        public string Id => id;

        public NpcProfile ToProfile() => new NpcProfile(new NpcId(id), displayNameKey, role);

        public NpcSchedule ToSchedule() => new NpcSchedule(
            schedule.Where(s => !string.IsNullOrEmpty(s.nodeId)).Select(s => new NpcScheduleEntry(s.phase, s.nodeId)).ToList(),
            string.IsNullOrEmpty(defaultNodeId) ? "home" : defaultNodeId);
    }
}
