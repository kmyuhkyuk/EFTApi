﻿using System.Threading.Tasks;
using EFTReflection;

namespace EFTApi.Helpers
{
    public class MainMenuControllerHelper
    {
        public static readonly MainMenuControllerHelper Instance = new MainMenuControllerHelper();

        public MainMenuController MainMenuController { get; private set; }

        /// <summary>
        ///     Init Action
        /// </summary>
        public readonly RefHelper.HookRef Unsubscribe;

        public readonly RefHelper.HookRef Execute;

        private MainMenuControllerHelper()
        {
            Unsubscribe = new RefHelper.HookRef(typeof(MainMenuController), "Unsubscribe");
            Execute = new RefHelper.HookRef(typeof(MainMenuController), "Execute");

            Execute.Add(this, nameof(OnExecute));
        }

        private static async void OnExecute(Task<MainMenuController> __result)
        {
            Instance.MainMenuController = await __result;
        }
    }
}