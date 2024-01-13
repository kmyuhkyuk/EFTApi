﻿using EFTConfiguration.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EFTConfiguration.Views.Components.Base
{
    public class ConfigDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string modName;

        public string descriptionNameKey;

        public void OnPointerEnter(PointerEventData eventData)
        {
            EFTConfigurationView.EnableDescription(LocalizedHelper.Localized(modName, descriptionNameKey));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EFTConfigurationView.DisableDescription();
        }
    }
}