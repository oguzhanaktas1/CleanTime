using System;
using System.Collections.Generic;
using UnityEngine;
using CleanTime.NPC.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.NPC.Presentation
{
    /// <summary>Moves NPC views to the anchor of their current schedule node. Far-away NPCs need no view at all.</summary>
    public sealed class NpcScheduleController : MonoBehaviour, ISceneService
    {
        [Serializable]
        private struct NodeAnchor
        {
            public string nodeId;
            public Transform anchor;
        }

        [SerializeField] private NodeAnchor[] anchors = new NodeAnchor[0];
        [SerializeField] private NpcView[] npcs = new NpcView[0];

        private IDisposable _subscription;

        public void Construct(IServiceResolver services)
        {
            var repository = services.Resolve<INpcRepository>();

            foreach (var npc in repository.GetAll())
                Place(npc.Id, npc.CurrentNodeId);

            _subscription = services.Resolve<IEventBus>().Subscribe<NpcScheduleNodeChangedEvent>(e => Place(e.NpcId, e.NodeId));
        }

        private void OnDestroy() => _subscription?.Dispose();

        private void Place(NpcId id, string nodeId)
        {
            var view = Array.Find(npcs, n => n != null && n.NpcId == id);
            var node = Array.Find(anchors, a => a.nodeId == nodeId);

            if (view != null && node.anchor != null)
                view.PlaceAt(node.anchor.position);
        }
    }
}
