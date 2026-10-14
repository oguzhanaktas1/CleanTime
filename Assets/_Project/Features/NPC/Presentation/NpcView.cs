using System;
using System.Collections.Generic;
using UnityEngine;
using CleanTime.NPC.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.NPC.Presentation
{
    /// <summary>Visual body of an NPC in the town. Which NPC it is comes from a stable business id.</summary>
    public sealed class NpcView : MonoBehaviour
    {
        [SerializeField] private string npcId = "";

        public NpcId NpcId => new NpcId(npcId ?? string.Empty);

        public void PlaceAt(Vector3 position) => transform.position = position;
    }
}
