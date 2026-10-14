using UnityEngine;
using CleanTime.Gifts.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/Gift Definition", fileName = "SO_Gift_")]
    public sealed class GiftDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private long priceCents;
        [Tooltip("Empty = any NPC accepts this gift.")]
        [SerializeField] private string recipientNpcId = "";
        [Min(0)] [SerializeField] private int relationshipGain = 10;
        [SerializeField] private string dialogueUnlockKey = "";
        [Min(0)] [SerializeField] private int minRelationshipLevel;

        public string Id => id;

        public GiftDefinitionSnapshot ToSnapshot() => new GiftDefinitionSnapshot(
            new GiftId(id),
            displayNameKey,
            new Money(priceCents),
            new NpcId(recipientNpcId ?? string.Empty),
            new GiftEffect(relationshipGain, dialogueUnlockKey),
            new GiftRequirement(minRelationshipLevel));
    }
}
