using UnityEngine;

public static class TutorialDirector
{
    public static void Reset()
    {
        TutorialScript.Reset();
        TutorialCombatScript.Reset();
        TutorialPresenter presenter = TutorialPresenter.Ensure();
        if (presenter != null)
            presenter.ResetRun();
    }

    public static void Raise(TutorialTrigger trigger, EncounterType encounter = EncounterType.None, int arg = -1)
    {
        if (Controller.Instance == null || !Controller.Instance.IsTutorialRun)
            return;

        TutorialPresenter presenter = TutorialPresenter.Ensure();
        if (presenter == null)
            return;

        for (int i = 0; i < TutorialScript.Beats.Count; i++)
        {
            TutorialBeat beat = TutorialScript.Beats[i];
            if (beat.Played || beat.Trigger != trigger)
                continue;
            if (beat.Encounter != EncounterType.None && beat.Encounter != encounter)
                continue;
            if (beat.Arg >= 0 && beat.Arg != arg)
                continue;
            if (!TutorialScript.HasPlayed(beat.RequiresId))
                continue;

            beat.Played = true;
            presenter.Enqueue(beat);
        }
    }
}
