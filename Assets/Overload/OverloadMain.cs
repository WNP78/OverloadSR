using ModApi;
using ModApi.Design;
using ModApi.Ui;
using System.Xml.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using ModApi.Craft.Parts;

namespace WNP78.Overload
{
    public static class OverloadMain
    {
        static IDesigner Designer;
        static Type DesignerScript;
        static Type CraftBuilder;
        static string DialogXML;
        public static void Init()
        {
            ServiceProvider.Instance.Game.SceneManager.SceneLoaded += OnSceneLoaded;
            CraftBuilder = ReflectionUtils.GetType("Assets.Scripts.Craft.CraftBuilder");
            DesignerScript = ReflectionUtils.GetType("Assets.Scripts.Design.DesignerScript");
            DialogXML = ServiceProvider.Instance.ResourceLoader.LoadAsset<TextAsset>("Assets/Dialog.xml").text;
        }
        static void OnSceneLoaded(object sender, ModApi.Scenes.Events.SceneEventArgs e)
        {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                Designer = ServiceProvider.Instance.Game.Designer;
                var flyout = Designer.DesignerUi.Flyouts.PartProperties;
                flyout.Opened += PartPropertiesOpened;
                Designer.SelectedPartChanged += SelectedPartChanged;
            }
            else
            {
                Designer = null;
            }
        }
        static void PartPropertiesOpened(IFlyout flyout)
        {
            Debug.Log("openend: " + flyout.Transform);
        }
        static void SelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            
        }
    }
}