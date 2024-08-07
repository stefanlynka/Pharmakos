using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence
{
    List<SequenceItem> sequenceItems = new List<SequenceItem>();
    int currentSequence = 0;

    public void Add(SequenceItem sequenceItem)
    {
        sequenceItems.Add(sequenceItem);
    }

    // Return true when the sequence is complete
    public bool Progress()
    {
        if (currentSequence >= sequenceItems.Count)
        {
            Clear();
            return true;
        }

        SequenceItem item = sequenceItems[currentSequence];

        while (item.Progress())
        {
            currentSequence++;
            if (currentSequence >= sequenceItems.Count)
            {
                Clear();
                return true;
            }
            item = sequenceItems[currentSequence];
        }

        return false;
    }

    public void Start()
    {
        SequenceHandler.instance.StartSequence(this);
    }
    public void Clear()
    {
        sequenceItems.Clear();
        //SequenceManager.instance.RemoveSequence(this);
        //OnFinish = null;
    }
}

public interface SequenceItem
{
    public bool Progress();
}

public class SequenceAction : SequenceItem
{
    private Action action;
    public SequenceAction(Action action)
    {
        this.action = action; 
    }
    public bool Progress()
    {
        action.Invoke();
        return true;
    }
}