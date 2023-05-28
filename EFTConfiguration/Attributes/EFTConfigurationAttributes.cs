﻿using System;

namespace EFTConfiguration.Attributes
{
    // ReSharper disable InvalidXmlDocComment
    /// <summary>
    ///     You can copy this file to any project and Declare Attribute, EFTConfigurationManager will auto search Attribute by
    ///     name and fields
    /// </summary>
    /// <remarks>
    ///     EFTConfigurationManager will auto search and copy ConfigurationManagerAttributes Value
    /// </remarks>
    public sealed class EFTConfigurationAttributes
    {
        /// <summary>
        ///     Never displayed Setting
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.Browsable" /> Value
        /// </remarks>
        public bool HideSetting;

        /// <summary>
        ///     Never displayed Rest Button
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.HideDefaultButton" />
        /// </remarks>
        public bool HideRest;

        /// <summary>
        ///     Never displayed Slider
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.ShowRangeAsPercent" />
        /// </remarks>
        public bool HideRange;

        /// <summary>
        ///     The Setting is ReadOnly
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.ReadOnly" />
        /// </remarks>
        public bool ReadOnly;

        /// <summary>
        ///     The Setting is Advanced
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.IsAdvanced" />
        /// </remarks>
        public bool Advanced;

        /// <summary>
        ///     Bind Action to String type Custom Button
        /// </summary>
        /// <remarks>
        ///     Will not copy because <see cref="ConfigurationManagerAttributes.CustomDrawer" /> is Draw OnGUI Action
        /// </remarks>
        public Action ButtonAction;

        /// <summary>
        ///     If your need add Custom Type Setting, Bind this <see cref="object" /> converted <see cref="string" />
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.ObjToStr" />
        /// </remarks>
        public Func<object, string> CustomToString;

        /// <summary>
        ///     If your need add Custom Type Setting, Bind this <see cref="string" /> converted <see cref="object" />
        /// </summary>
        /// <remarks>
        ///     Copy <see cref="ConfigurationManagerAttributes.StrToObj" />
        /// </remarks>
        public Func<string, object> CustomToObject;
    }
}