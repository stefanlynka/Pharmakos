using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanPlayer : Player
{
    public HumanPlayer()
    {
        IsHuman = true;
    }

    public override void StartTurn()
    {
        base.StartTurn();
        if (Controller.Instance != null
            && Controller.Instance.IsTutorialRun
            && Controller.Instance.CanonGameState != null
            && ReferenceEquals(Controller.Instance.CanonGameState.CurrentPlayer, this))
            TutorialDirector.Raise(TutorialTrigger.CombatTurnStarted);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
