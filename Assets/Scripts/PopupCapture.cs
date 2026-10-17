using UnityEngine;

public class PopupCapture : MonoBehaviour
{
    public GameObject Container;
    public PopupType CurrentPopup;

    public ViewFollower ViewFollower;
    public ViewSpell ViewSpell;
    public ViewRitual ViewRitual;
    public ViewBuff ViewTrinket;
    public GameObject ViewHeartstring;

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

        if (ViewRitual != null)
        {
            ViewRitual.gameObject.SetActive(true);
            ViewRitual.Init(ritual, clickable: false);
        }
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

        if (ViewTrinket != null)
        {
            ViewTrinket.SetSummaryForced(false);
            ViewTrinket.SetVisible(false);
        }

        if (ViewHeartstring != null)
            ViewHeartstring.SetActive(false);
    }
}
