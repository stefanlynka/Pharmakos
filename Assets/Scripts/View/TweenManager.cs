using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class TweenManager
//{
//    public static TweenManager instance;
//    public List<Tween> Tweens = new List<Tween>();

//    public TweenManager() 
//    {
//        if (instance == null) instance = this;
//    }

//    public void StartTween(Tween newTween)
//    {
//        Tweens.Add(newTween);
//    }
//    public void RemoveTween(Tween newTween)
//    {
//        Tweens.Remove(newTween);
//    }
//    public void Update()
//    {
//        for (int i = Tweens.Count - 1; i >= 0; i--)
//        {
//            Tween tween = Tweens[i];
//            tween.Progress();
//            if (tween.TimeRemaining <= 0)
//            {
//                if (tween.OnFinish != null) tween.OnFinish();
//                tween.OnFinish = null;
//                Tweens.RemoveAt(i);
//            }
//        }
//    }
//}
