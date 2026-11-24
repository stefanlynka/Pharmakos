#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only: plays ritual invocations in combat without paying for them, for tuning RitualThemes and the sounds.
/// Start a combat (e.g. Ctrl+Shift+T on the main menu for the test chamber), then use Tools → Rituals → Preview ...
/// The invocation plays from your Major altar onto the first enemy follower (or the enemy portrait).
/// Not in builds.
/// </summary>
public class RitualInvocationPreview : MonoBehaviour
{
    static RitualInvocationPreview runner;

    public static readonly string[] AllRituals =
    {
        "ZeusMajor", "ZeusMinor", "PoseidonMajor", "PoseidonMinor", "HadesMajor", "HadesMinor", "AresMajor", "AresMinor",
        "AthenaMajor", "ApolloMajor", "ApolloMinor", "AphroditeMajor", "AphroditeMinor", "DemeterMajor", "DemeterMinor",
        "DionysusMinor", "HephaestusMajor", "HeraMajor", "HermesMajor", "HermesMinor", "HestiaMinor", "MuseMinor", "OldOnesMinor",
    };

    [MenuItem("Tools/Rituals/Preview Every Ritual Invocation (Play Mode)")]
    static void PreviewAllMenu() => PlayAll(4f);

    [MenuItem("Tools/Rituals/Preview Every Ritual Invocation (Play Mode)", true)]
    static bool CanPreview() => Application.isPlaying && Controller.Instance != null && Controller.Instance.GameRunning;

    /// <summary>Plays one ritual's invocation. targetId &lt; 0 picks the first enemy follower, else the enemy portrait.</summary>
    public static string Play(string ritualClass, int targetId = -1, float[] captureTimes = null, string captureFolder = null)
    {
        Ritual ritual = MakeRitual(ritualClass);
        if (ritual == null)
            return "No ritual class " + ritualClass;
        ViewTarget target = FindTarget(targetId);
        if (target == null)
            return "No target on the board";
        ViewTarget source = FindSource();

        new RitualInvocationVfx(ritual).Play(new VfxContext(source, target), null);
        if (captureTimes != null && captureTimes.Length > 0)
            Runner.StartCoroutine(Capture(ritualClass, captureTimes, captureFolder ?? "Claude/RitualFX/Shots"));
        return "Playing " + ritualClass + " on " + target.name;
    }

    /// <summary>Plays every ritual in turn. With captureFolder set, screenshots each one mid-gathering, on the boon and in the tail.</summary>
    public static void PlayAll(float interval, string captureFolder = null, int targetId = -1)
    {
        Runner.StopAllCoroutines();
        Runner.StartCoroutine(PlayAllRoutine(interval, captureFolder, targetId));
    }

    static IEnumerator PlayAllRoutine(float interval, string captureFolder, int targetId)
    {
        foreach (string name in AllRituals)
        {
            Debug.Log("Ritual preview: " + name);
            float climax = new RitualInvocationVfx(MakeRitual(name)).Climax;
            Play(name, targetId, captureFolder != null ? new[] { climax * 0.7f, climax + 0.06f, climax + 0.4f } : null, captureFolder);
            yield return new WaitForSeconds(interval);
        }
        Debug.Log("Ritual preview: done");
    }

    static IEnumerator Capture(string name, float[] times, string folder)
    {
        Directory.CreateDirectory(folder);
        float start = Time.time;
        List<float> sorted = new List<float>(times);
        sorted.Sort();
        foreach (float t in sorted)
        {
            while (Time.time - start < t)
                yield return null;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(folder, name + "_" + Mathf.RoundToInt(t * 100).ToString("000") + ".png")));
        }
    }

    static RitualInvocationPreview Runner
    {
        get
        {
            if (runner == null)
                runner = new GameObject("RitualInvocationPreview").AddComponent<RitualInvocationPreview>();
            return runner;
        }
    }

    static Ritual MakeRitual(string className)
    {
        Type type = typeof(Ritual).Assembly.GetType(className);
        if (type == null || !typeof(Ritual).IsAssignableFrom(type))
            return null;
        Ritual ritual = (Ritual)Activator.CreateInstance(type);
        ritual.Owner = Controller.Instance.Player1;
        return ritual;
    }

    static ViewTarget FindTarget(int targetId)
    {
        if (targetId >= 0)
            return View.Instance.GetViewTargetByID(targetId);
        Player enemy = Controller.Instance.Player2;
        foreach (Follower f in enemy.BattleRow.Followers)
        {
            ViewTarget v = View.Instance.GetViewTargetByID(f.GetID());
            if (v != null)
                return v;
        }
        return View.Instance.GetViewTargetByID(enemy.GetID());
    }

    static ViewTarget FindSource()
    {
        Player p = Controller.Instance.Player1;
        Ritual altar = p.MajorRitual ?? p.MinorRitual;
        return altar != null ? View.Instance.GetViewTargetByID(altar.GetID()) : null;
    }
}
#endif
