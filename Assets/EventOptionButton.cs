using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pharmakos.Events.UI
{
    [RequireComponent(typeof(Button))]
    public class EventOptionButtonComponent : MonoBehaviour
    {
        public TextMeshProUGUI OptionText;
        public Button Button;

        Action _onClick;

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

        public void Load(string optionText, Action onClick)
        {
            if (OptionText != null)
                OptionText.text = optionText;

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
            gameObject.SetActive(false);
        }

        void HandleClicked()
        {
            _onClick?.Invoke();
        }
    }
}
