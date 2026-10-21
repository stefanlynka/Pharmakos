using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deck picker on <see cref="ScreenName.SelectableDeckViewerScreen"/>.
/// One card can be selected. Continue copies nothing until the caller decides what the selection means.
/// </summary>
public class SelectableDeckViewerScreenHandler : MonoBehaviour
{
    public Button ContinueButton;

    ContentScrollView _viewer;
    Action<Card> _onContinue;

    void Awake()
    {
        if (ContinueButton == null)
            ContinueButton = GetComponentInChildren<Button>(true);

        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(Continue);
    }

    void OnDestroy()
    {
        if (ContinueButton != null)
            ContinueButton.onClick.RemoveListener(Continue);
    }

    public void Show(List<Card> cards, Action<Card> onContinue)
    {
        _onContinue = onContinue;
        _viewer = FindViewer();

        ContentScrollScreenHandler scrollScreen = GetComponent<ContentScrollScreenHandler>();
        if (scrollScreen == null)
            scrollScreen = gameObject.AddComponent<ContentScrollScreenHandler>();
        scrollScreen.BindRenderTexture();

        if (_viewer == null)
        {
            Debug.LogError("SelectableDeckViewerScreen: no selectable deck viewer area.");
            Finish(null);
            return;
        }

        _viewer.ShowSelectableCards(cards, scrollScreen.ContentView);
    }

    public void Hide()
    {
        _onContinue = null;
        if (_viewer != null)
            _viewer.HideView();
    }

    void Continue()
    {
        Card selected = _viewer != null ? _viewer.SelectedCard : null;
        Finish(selected);
    }

    void Finish(Card selected)
    {
        Action<Card> callback = _onContinue;
        _onContinue = null;
        if (_viewer != null)
            _viewer.HideView();
        callback?.Invoke(selected);
    }

    ContentScrollView FindViewer()
    {
        if (_viewer != null)
            return _viewer;

        ContentScrollView[] viewers = FindObjectsOfType<ContentScrollView>(true);
        foreach (ContentScrollView viewer in viewers)
        {
            if (viewer != null && viewer.IsSelectableCopy)
                return viewer;
        }

        return null;
    }
}
