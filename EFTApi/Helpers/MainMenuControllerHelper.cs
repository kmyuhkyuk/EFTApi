﻿using System;
using System.Threading.Tasks;
using EFTReflection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace EFTApi.Helpers
{
    public class MainMenuControllerHelper
    {
        private static readonly Lazy<MainMenuControllerHelper> Lazy =
            new Lazy<MainMenuControllerHelper>(() => new MainMenuControllerHelper());

        public static MainMenuControllerHelper Instance => Lazy.Value;

        public MainMenuController MainMenuController { get; private set; }

        /// <summary>
        ///     Init Action
        /// </summary>
        public readonly RefHelper.HookRef Execute;

        /// <summary>
        ///     Unsubscribe Action
        /// </summary>
        public readonly RefHelper.HookRef Unsubscribe;

        private MainMenuControllerHelper()
        {
            var mainMenuControllerType = typeof(MainMenuController);

            Execute = RefHelper.HookRef.Create(mainMenuControllerType, "Execute");
            Unsubscribe = RefHelper.HookRef.Create(mainMenuControllerType, "Unsubscribe");

            Execute.Add(this, nameof(OnExecute));
        }

        private static async void OnExecute(Task<MainMenuController> __result)
        {
            Instance.MainMenuController = await __result;
        }
    }
}