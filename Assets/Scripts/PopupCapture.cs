using TMPro;
using UnityEngine;

public class PopupCapture : MonoBehaviour
{
    const string CurrentRitualPrefabPath = "Prefabs/View/ViewRitual3";
    const float RitualFrameFill = 0.82f;

    public GameObject Container;
    public PopupType CurrentPopup;

    public ViewFollower ViewFollower;
    public ViewSpell ViewSpell;
    public ViewRitual ViewRitual;
    public ViewBuff ViewTrinket;
    public GameObject ViewHeartstring;

    ViewRitual shownRitual;

    public void Clear()
    {
        HideAllViews();
        CurrentPopup = PopupType.None;
    }

    public void LoadCard(Card card)
    {
        if (card == null)
        {
            Clear();
            return;
        }

        HideAllViews();

        if (card is Follower)
        {
            CurrentPopup = PopupType.Follower;
            if (ViewFollower != null)
            {
                ViewFollower.gameObject.SetActive(true);
                ViewFollower.Load(card);
                ViewFollower.ShowMaxStats();
                ViewFollower.SetDescriptiveMode(true);
                if (ViewFollower.CardCollider != null)
                    ViewFollower.CardCollider.enabled = false;
            }
        }
        else if (card is Spell)
        {
            CurrentPopup = PopupType.Spell;
            if (ViewSpell != null)
            {
                ViewSpell.gameObject.SetActive(true);
                ViewSpell.Load(card);
                ViewSpell.EnterCardMode();
                ViewSpell.SetDescriptiveMode(true);
                if (ViewSpell.CardCollider != null)
                    ViewSpell.CardCollider.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"PopupCapture: unsupported card type {card.GetType().Name}");
            CurrentPopup = PopupType.None;
        }
    }

    public void LoadRitual(Ritual ritual)
    {
        HideAllViews();
        CurrentPopup = PopupType.Ritual;

        ViewRitual view = GetShownRitual();
        if (view == null)
            return;

        view.gameObject.SetActive(true);
        view.Init(ritual, clickable: false);
        FrameRitual(view);
    }

    public void LoadTrinket(Trinket trinket)
    {
        if (trinket == null)
        {
            Clear();
            return;
        }

        LoadBuff(trinket.MyEffect, trinket.GetDescriptionData());
        CurrentPopup = PopupType.Trinket;
    }

    public void LoadBuff(StaticPlayerEffect buff, PlayerEffectDescriptionData descriptionData = null, int amount = 1)
    {
        if (buff == null && descriptionData == null)
        {
            Clear();
            return;
        }

        HideAllViews();
        CurrentPopup = PopupType.Trinket;

        if (ViewTrinket != null)
        {
            if (descriptionData == null && buff != null)
                descriptionData = buff.GetDescriptionData();

            ViewTrinket.SetBuffData(buff, descriptionData);
            ViewTrinket.SetAmount(amount);
            ViewTrinket.SetHighlight(false);
            ViewTrinket.SetVisible(true);
            ViewTrinket.SetSummaryForced(true);
        }
    }

    public void LoadHeartstring()
    {
        HideAllViews();
        CurrentPopup = PopupType.Heartstring;

        if (ViewHeartstring != null)
            ViewHeartstring.SetActive(true);
    }

    void HideAllViews()
    {
        if (ViewFollower != null)
            ViewFollower.gameObject.SetActive(false);

        if (ViewSpell != null)
            ViewSpell.gameObject.SetActive(false);

        if (ViewRitual != null)
            ViewRitual.gameObject.SetActive(false);

        if (shownRitual != null && shownRitual != ViewRitual)
            shownRitual.gameObject.SetActive(false);

        if (ViewTrinket != null)
        {
            ViewTrinket.SetSummaryForced(false);
            ViewTrinket.SetVisible(false);
        }

        if (ViewHeartstring != null)
            ViewHeartstring.SetActive(false);
    }

    // The capture still holds the old ritual as a placement anchor. The preview uses ViewRitual3.
    ViewRitual GetShownRitual()
    {
        if (shownRitual != null)
            return shownRitual;

        GameObject prefab = Resources.Load<GameObject>(CurrentRitualPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("PopupCapture: missing Resources/" + CurrentRitualPrefabPath);
            return ViewRitual;
        }

        Transform parent = ViewRitual != null ? ViewRitual.transform.parent : transform;
        GameObject ritualObject = Instantiate(prefab, parent, false);
        ritualObject.name = "ViewRitual";
        shownRitual = ritualObject.GetComponent<ViewRitual>();
        if (shownRitual == null)
        {
            Destroy(ritualObject);
            return ViewRitual;
        }

        if (ViewRitual != null)
        {
            ritualObject.transform.localPosition = ViewRitual.transform.localPosition;
            ritualObject.transform.localRotation = ViewRitual.transform.localRotation;
            ViewRitual.gameObject.SetActive(false);
        }

        int layer = CaptureLayer();
        SetLayerRecursively(ritualObject, layer);
        RestrictLightsToLayer(ritualObject, layer);

        Collider[] colliders = ritualObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        shownRitual.gameObject.SetActive(false);
        return shownRitual;
    }

    void FrameRitual(ViewRitual view)
    {
        if (view == null || view == ViewRitual)
            return;

        if (ViewRitual != null)
        {
            view.transform.localPosition = ViewRitual.transform.localPosition;
            view.transform.localRotation = ViewRitual.transform.localRotation;
        }

        view.SetDisplayScale(Vector3.one);

        TextMeshPro[] labels = view.GetComponentsInChildren<TextMeshPro>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].gameObject.activeInHierarchy)
                labels[i].ForceMeshUpdate();
        }

        if (!TryGetRendererBounds(view, out Bounds bounds))
            return;

        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null)
            return;

        Transform space = view.transform.parent != null ? view.transform.parent : transform;
        Vector3 cameraLocal = space.InverseTransformPoint(cam.transform.position);
        Vector3 centerLocal = space.InverseTransformPoint(bounds.center);
        float distance = centerLocal.z - cameraLocal.z;
        if (distance < 0.5f)
            distance = 11.6f;

        float visibleHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float aspect = 1f;
        if (cam.targetTexture != null && cam.targetTexture.height > 0)
            aspect = cam.targetTexture.width / (float)cam.targetTexture.height;
        else if (cam.aspect > 0.01f)
            aspect = cam.aspect;

        float visibleWidth = visibleHeight * aspect;
        float fit = Mathf.Min(
            visibleWidth * RitualFrameFill / Mathf.Max(0.01f, bounds.size.x),
            visibleHeight * RitualFrameFill / Mathf.Max(0.01f, bounds.size.y));
        view.SetDisplayScale(Vector3.one * fit);

        if (!TryGetRendererBounds(view, out bounds))
            return;

        centerLocal = space.InverseTransformPoint(bounds.center);
        Vector3 local = view.transform.localPosition;
        local.x += cameraLocal.x - centerLocal.x;
        local.y += cameraLocal.y - centerLocal.y;
        view.transform.localPosition = local;
    }

    int CaptureLayer()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null)
            return ViewRitual != null ? ViewRitual.gameObject.layer : 0;

        int bits = cam.cullingMask;
        for (int i = 0; i < 32; i++)
        {
            if ((bits & (1 << i)) != 0)
                return i;
        }

        return ViewRitual != null ? ViewRitual.gameObject.layer : 0;
    }

    static bool TryGetRendererBounds(ViewRitual view, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        Renderer[] renderers = view.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static void RestrictLightsToLayer(GameObject target, int layer)
    {
        if (target == null || layer < 0 || layer > 31)
            return;

        int mask = 1 << layer;
        Light[] lights = target.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
            lights[i].cullingMask = mask;
    }
}
