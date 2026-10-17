using System;
using Pharmakos.Events;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pharmakos.Events.UI
{
    [RequireComponent(typeof(Button))]
    public class EventOptionButtonComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI OptionText;
        public Button Button;

        Action _onClick;
        EventOutcomeData _outcome;
        EventScreenHandler _screenHandler;

        void Awake()
        {
            if (Button == null)
                Button = GetComponent<Button>();
            if (Button != null)
                Button.onClick.AddListener(HandleClicked);
        }

        void OnDestroy()
        {
            if (Button != null)
                Button.onClick.RemoveListener(HandleClicked);
        }

        public void Load(string optionText, EventOutcomeData outcome, EventScreenHandler screenHandler, Action onClick)
        {
            if (OptionText != null)
                OptionText.text = optionText;

            _outcome = outcome;
            _screenHandler = screenHandler;
            _onClick = onClick;
            gameObject.SetActive(true);
        }

        public void SetInteractable(bool isInteractable)
        {
            if (Button != null)
                Button.interactable = isInteractable;
        }

        /// <summary>Clears the click handler and hides the row. Does not remove the persistent UI listener on <see cref="Button"/>.</summary>
        public void ResetOption()
        {
            _onClick = null;
            _outcome = null;
            _screenHandler = null;
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Button != null && !Button.interactable)
                return;

            _screenHandler?.ShowOutcomePreview(_outcome);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _screenHandler?.HideOutcomePreview();
        }

        void HandleClicked()
        {
            _screenHandler?.HideOutcomePreview();
            _onClick?.Invoke();
        }
    }
}
