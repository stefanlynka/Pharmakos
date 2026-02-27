using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RitualRewardHandler : MonoBehaviour
{
    public ViewRitual CurrentTopRitual;
    public ViewRitual CurrentBottomRitual;

    public RitualReward TopRitualReward;
    public RitualReward BottomRitualReward;

    Ritual DefaultTopRitual;
    Ritual DefaultBottomRitual;

    List<Ritual> possibleRewards = new List<Ritual>();

    public void Load(int levelCompleted, Ritual topRitual, Ritual bottomRitual)
    {
        DefaultTopRitual = topRitual;
        DefaultBottomRitual = bottomRitual;

        CurrentTopRitual.Init(topRitual);
        CurrentBottomRitual.Init(bottomRitual);


        System.Type type1 = topRitual != null ? topRitual.GetType() : null;
        System.Type type2 = bottomRitual != null ? bottomRitual.GetType() : null;
        possibleRewards = Controller.Instance.ProgressionHandler.GetPossibleRitualRewards(); //CardHandler.GetPossibleRitualRewards(levelCompleted);
        possibleRewards.RemoveAll(ritual => ritual.GetType() == type1 || ritual.GetType() == type2);

        int randomIndex = Controller.Instance.MetaRNG.Next(0, possibleRewards.Count);
        TopRitualReward.Load(possibleRewards[randomIndex], 0, RitualButtonClicked);
        possibleRewards.RemoveAt(randomIndex);

        randomIndex = Controller.Instance.MetaRNG.Next(0, possibleRewards.Count);
        BottomRitualReward.Load(possibleRewards[randomIndex], 1, RitualButtonClicked);
        possibleRewards.RemoveAt(randomIndex);
    }

    public void SetRitualsAndContinue()
    {
        Controller.Instance.SetRituals(CurrentTopRitual.Ritual, CurrentBottomRitual.Ritual);

        //DeselectAll();

        Controller.Instance.StartNextLevel();

        //gameObject.SetActive(false);

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(Wait, 0, 1, 0.8f));
        moveSequence.Add(new SequenceAction(Cleanup));
        moveSequence.Start();
    }
    private void Wait(float progress)
    {

    }
    private void Cleanup()
    {
        DeselectAll();

        gameObject.SetActive(false);
    }

    private void DeselectAll()
    {
        TopRitualReward.ChooseTopRitualButton.SetHighlight(false);
        TopRitualReward.ChooseBottomRitualButton.SetHighlight(false);

        BottomRitualReward.ChooseTopRitualButton.SetHighlight(false);
        BottomRitualReward.ChooseBottomRitualButton.SetHighlight(false);
    }

    public void RitualButtonClicked(ViewTarget target)
    {
        ChooseRitualRewardButton chooseRitualRewardButton = target as ChooseRitualRewardButton;
        if (chooseRitualRewardButton == null) return;

        bool isSelected = chooseRitualRewardButton.Selected;
        ViewRitual targetedViewRitual = chooseRitualRewardButton.ReplacementIndex == 0 ? CurrentTopRitual : CurrentBottomRitual;
        ViewRitual notTargetedViewRitual = chooseRitualRewardButton.ReplacementIndex == 0 ? CurrentBottomRitual : CurrentTopRitual;

        RitualReward currentRitualReward = chooseRitualRewardButton.RewardIndex == 0 ? TopRitualReward : BottomRitualReward;
        RitualReward otherRitualReward = chooseRitualRewardButton.RewardIndex == 0 ? BottomRitualReward : TopRitualReward;

        ChooseRitualRewardButton sameRewardOtherButton = chooseRitualRewardButton.ReplacementIndex == 0 ? currentRitualReward.ChooseBottomRitualButton : currentRitualReward.ChooseTopRitualButton;
        ChooseRitualRewardButton otherRewardSameButton = chooseRitualRewardButton.ReplacementIndex == 0 ? otherRitualReward.ChooseTopRitualButton : otherRitualReward.ChooseBottomRitualButton;

        Ritual defaultRitual = chooseRitualRewardButton.ReplacementIndex == 0 ? DefaultTopRitual : DefaultBottomRitual;
        Ritual otherDefaultRitual = chooseRitualRewardButton.ReplacementIndex == 0 ? DefaultBottomRitual : DefaultTopRitual;

        if (isSelected) // If it's selected, deselect it
        {
            chooseRitualRewardButton.SetHighlight(false);
            targetedViewRitual.Init(defaultRitual);
        }
        else
        {
            // Select this button
            chooseRitualRewardButton.SetHighlight(true);
            // Deselect other button on this ritual reward
            if (sameRewardOtherButton.Selected)
            {
                sameRewardOtherButton.SetHighlight(false);
                notTargetedViewRitual.Init(otherDefaultRitual);
            }

            // Also deselect the same button on the other ritual reward
            if (otherRewardSameButton.Selected)
            {
                otherRewardSameButton.SetHighlight(false);
                //targetedViewRitual.Init(defaultRitual);
            }
            // Set Current ViewRitual
            targetedViewRitual.Init(chooseRitualRewardButton.Ritual);
        }
    }
}
