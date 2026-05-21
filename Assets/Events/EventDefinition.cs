using System.Collections.Generic;
using UnityEngine;

namespace Pharmakos.Events
{
    [System.Serializable]
    public class EventOptionData
    {
        public string OptionText;
        public EventOutcomeData Outcome = new EventOutcomeData();
    }

    [System.Serializable]
    public class EventOutcomeData
    {
        public EventOutcomeType OutcomeType = EventOutcomeType.None;
        public List<string> RewardInfo = new();
    }


    [CreateAssetMenu(fileName = "New Event", menuName = "Pharmakos/Event Definition", order = 0)]
    public class EventDefinition : ScriptableObject
    {
        [TextArea(3, 8)]
        public string EventText;

        public Sprite BackgroundImage;
        public Vector2 ImageStartingPosition;
        public Vector3 ImageStartingScale;
        public EventSummaryPosition SummaryPosition;

        public List<EventOptionData> Options = new List<EventOptionData>();
    }

    public enum EventName
    {
        Prometheus,
        Narcissus,
        Medusa,
        Tiresias,
        Pygmalion,
        Penelope,
        Lynceus,
        Leander,
        Psyche,
        Pandora,
        Bellerophon,
        Demeter,
    }
    public enum EventOutcomeType
    {
        None,
        GainCard,
        GainTrinket,
        GainRituals,
        FightEagle,
        IncreaseMaxHealth,
        CopyCardInDeck,
        RemoveCardFromDeck,
        GainHeartstring,
        BecomeLastEnemy,
        GoldenFleeceFight,
        RebukeTheGods,
        
    }
}
