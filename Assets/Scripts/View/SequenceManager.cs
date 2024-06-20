using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SequenceManager
{
    public static SequenceManager instance;
    public List<Sequence> Sequences = new List<Sequence>();

    public SequenceManager() 
    {
        if (instance == null) instance = this;
    }

    public void StartSequence(Sequence sequence)
    {
        Sequences.Add(sequence);
    }
    //public void RemoveSequence(Sequence sequence)
    //{
    //    Sequences.Remove(sequence);
    //}
    public void Update()
    {
        for (int i = Sequences.Count - 1; i >= 0; i--)
        {
            Sequence sequence = Sequences[i];
            bool sequenceComplete = sequence.Progress();
            if (sequenceComplete)
            {
                Sequences.RemoveAt(i);
            }
        }
    }
}
