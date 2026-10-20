using System.Collections.Generic;
using System.IO;
using Pharmakos.Events;
using UnityEngine;

public static class RunSave
{
    public const string FileName = "run-save.json";

    public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave()
    {
        return TryLoad(out _);
    }

    public static bool TryLoad(out RunSaveData save)
    {
        save = null;
        string path = Path;
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            save = JsonUtility.FromJson<RunSaveData>(json);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Run save: could not read " + path + " (" + exception.Message + ")");
            save = null;
            return false;
        }

        if (save == null || save.version != RunSaveData.CurrentVersion)
        {
            Debug.LogError("Run save: unsupported save version at " + path);
            save = null;
            return false;
        }

        return true;
    }

    public static void WriteFromCurrentRun()
    {
        Controller controller = Controller.Instance;
        if (controller == null || controller.HumanPlayerDetails == null) return;

        RunSaveData save = Capture(controller);
        string json = JsonUtility.ToJson(save, true);
        string path = Path;
        string temp = path + ".tmp";

        try
        {
            File.WriteAllText(temp, json);
            if (File.Exists(path))
            {
                string backup = path + ".bak";
                File.Replace(temp, path, backup);
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            else
            {
                File.Move(temp, path);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Run save: could not write " + path + " (" + exception.Message + ")");
        }
    }

    public static void Delete()
    {
        string path = Path;
        if (!File.Exists(path)) return;

        try
        {
            File.Delete(path);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Run save: could not delete " + path + " (" + exception.Message + ")");
        }
    }

    public static bool Apply(RunSaveData save)
    {
        Controller controller = Controller.Instance;
        if (controller == null || save == null || controller.HumanPlayerDetails == null || controller.ProgressionHandler == null)
            return false;

        controller.RunSeed = save.runSeed;
        controller.MetaRNG.SetSeed(save.metaRngSeed);
        controller.RunHeartStrings = save.heartstrings;
        ApplyPlayer(controller, save);
        ApplyStyx(controller, save);
        ApplySacrificedFollowers(controller, save);
        ApplyEncounteredEvents(controller, save);
        controller.ProgressionHandler.ReadRunProgress(save);

        if (controller.OverworldMapController != null)
            controller.OverworldMapController.ApplySavedMap(save);

        return true;
    }

    static RunSaveData Capture(Controller controller)
    {
        var save = new RunSaveData
        {
            version = RunSaveData.CurrentVersion,
            runSeed = controller.RunSeed,
            metaRngSeed = (int)controller.MetaRNG.CurrentSeed,
            heartstrings = controller.RunHeartStrings,
        };

        PlayerDetails details = controller.HumanPlayerDetails;
        save.deck = TypeNames(details.DeckBlueprint != null && details.DeckBlueprint.ContainsKey(0) ? details.DeckBlueprint[0] : null);
        save.majorRitual = RitualName(details.MajorRituals, 0);
        save.minorRitual = RitualName(details.MinorRituals, 0);
        save.trinkets = TypeNames(details.Trinkets != null && details.Trinkets.ContainsKey(0) ? details.Trinkets[0] : null);

        StyxRunState styx = controller.StyxRunState;
        if (styx != null)
        {
            save.styxCards = TypeNames(styx.RemovedCards);
            save.styxRituals = TypeNames(styx.RemovedRituals);
            save.styxTrinkets = TypeNames(styx.RemovedTrinkets);
            save.styxHasCrossed = styx.HasCrossed;
        }

        save.sacrificedFollowers = TypeNames(controller.SacrificedFollowers);

        save.encounteredEvents = new List<string>();
        if (controller.EncounteredEventsThisRun != null)
        {
            foreach (EventDefinition definition in controller.EncounteredEventsThisRun)
            {
                if (definition != null)
                    save.encounteredEvents.Add(definition.name);
            }
        }

        controller.ProgressionHandler?.WriteRunProgress(save);
        controller.OverworldMapController?.WriteMap(save);
        return save;
    }

    static void ApplyPlayer(Controller controller, RunSaveData save)
    {
        PlayerDetails details = controller.HumanPlayerDetails;
        if (details.DeckBlueprint == null)
            details.DeckBlueprint = new Dictionary<int, List<Card>>();
        details.DeckBlueprint[0] = CardsFromNames(save.deck);

        if (details.MajorRituals == null)
            details.MajorRituals = new Dictionary<int, Ritual>();
        if (details.MinorRituals == null)
            details.MinorRituals = new Dictionary<int, Ritual>();
        details.MajorRituals[0] = RunContent.CreateRitual(save.majorRitual);
        details.MinorRituals[0] = RunContent.CreateRitual(save.minorRitual);

        if (details.Trinkets == null)
            details.Trinkets = new Dictionary<int, List<Trinket>>();
        details.Trinkets[0] = TrinketsFromNames(save.trinkets);
        controller.RefreshLoadedRunFlags();
    }

    static void ApplyStyx(Controller controller, RunSaveData save)
    {
        if (controller.StyxRunState == null)
            controller.StyxRunState = new StyxRunState();

        controller.StyxRunState.Reset();
        foreach (Card card in CardsFromNames(save.styxCards))
            controller.StyxRunState.RecordRemovedCard(card);
        if (save.styxRituals != null)
        {
            foreach (string typeName in save.styxRituals)
            {
                Ritual ritual = RunContent.CreateRitual(typeName);
                if (ritual != null)
                    controller.StyxRunState.RecordRemovedRitual(ritual);
            }
        }
        if (save.styxTrinkets != null)
        {
            foreach (string typeName in save.styxTrinkets)
            {
                Trinket trinket = RunContent.CreateTrinket(typeName);
                if (trinket != null)
                    controller.StyxRunState.RecordRemovedTrinket(trinket);
            }
        }

        controller.StyxRunState.HasCrossed = save.styxHasCrossed;
    }

    static void ApplySacrificedFollowers(Controller controller, RunSaveData save)
    {
        controller.SacrificedFollowers.Clear();
        if (save.sacrificedFollowers == null) return;

        foreach (string typeName in save.sacrificedFollowers)
        {
            Follower follower = RunContent.CreateFollower(typeName);
            if (follower != null)
                controller.SacrificedFollowers.Add(follower);
        }
    }

    static void ApplyEncounteredEvents(Controller controller, RunSaveData save)
    {
        controller.EncounteredEventsThisRun.Clear();
        if (save.encounteredEvents == null || save.encounteredEvents.Count == 0) return;

        EventHandler events = controller.EventHandler;
        if (events == null)
            events = Object.FindObjectOfType<EventHandler>(true);
        if (events == null || events.Events == null)
        {
            Debug.LogWarning("Run save: no event list to restore encountered events.");
            return;
        }

        foreach (string eventName in save.encounteredEvents)
        {
            EventDefinition match = null;
            foreach (EventDefinition definition in events.Events)
            {
                if (definition != null && definition.name == eventName)
                {
                    match = definition;
                    break;
                }
            }

            if (match != null)
                controller.EncounteredEventsThisRun.Add(match);
            else
                Debug.LogWarning("Run save: missing event " + eventName);
        }
    }

    static List<Card> CardsFromNames(List<string> typeNames)
    {
        var cards = new List<Card>();
        if (typeNames == null) return cards;
        foreach (string typeName in typeNames)
        {
            Card card = RunContent.CreateCard(typeName);
            if (card != null)
                cards.Add(card);
        }
        return cards;
    }

    static List<Trinket> TrinketsFromNames(List<string> typeNames)
    {
        var trinkets = new List<Trinket>();
        if (typeNames == null) return trinkets;
        foreach (string typeName in typeNames)
        {
            Trinket trinket = RunContent.CreateTrinket(typeName);
            if (trinket != null)
                trinkets.Add(trinket);
        }
        return trinkets;
    }

    static List<string> TypeNames<T>(IEnumerable<T> items) where T : class
    {
        var names = new List<string>();
        if (items == null) return names;
        foreach (T item in items)
        {
            if (item != null)
                names.Add(item.GetType().Name);
        }
        return names;
    }

    static string RitualName(Dictionary<int, Ritual> rituals, int pool)
    {
        if (rituals == null || !rituals.TryGetValue(pool, out Ritual ritual) || ritual == null)
            return "";
        return ritual.GetType().Name;
    }
}
