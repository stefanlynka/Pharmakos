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
        public int CardCount = 1;
    }

    public enum EventOutcomeType
    {
        None,
        AddRandomTrinket,
        RemoveRandomCardFromDeck,
        DuplicateRandomCardInDeck,
    }

    public enum EventName
    {
        Prometheus,
        Pandora,
        Athena,
    }

    [CreateAssetMenu(fileName = "New Event", menuName = "Pharmakos/Event Definition", order = 0)]
    public class EventDefinition : ScriptableObject
    {
        [TextArea(3, 8)]
        public string EventText;

        public Sprite BackgroundImage;

        public List<EventOptionData> Options = new List<EventOptionData>();
    }
}
