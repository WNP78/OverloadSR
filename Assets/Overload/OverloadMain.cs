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
        static ConstructorInfo PartDataConstructor;
        public static void Init()
        {
            ServiceProvider.Instance.Game.SceneManager.SceneLoaded += OnSceneLoaded;
            CraftBuilder = ReflectionUtils.GetType("Assets.Scripts.Craft.CraftBuilder");
            DesignerScript = ReflectionUtils.GetType("Assets.Scripts.Design.DesignerScript");
            DialogXML = ServiceProvider.Instance.ResourceLoader.LoadAsset<TextAsset>("Assets/Dialog.xml").text;
            PartDataConstructor = typeof(PartData).GetConstructor(new Type[] { typeof(XElement), typeof(int), typeof(PartType) });
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

        static IPartScript part;
        static XElement backupXml;
        public static XElement GetXML()
        {
            part = Designer.SelectedPart;
            var res = part.Data.GenerateXml(part.Transform, false);
            backupXml = XElement.Parse(res.ToString(SaveOptions.DisableFormatting));
            return res;
        }
        public static void SaveXML(XElement xml)
        {
            var partData = part.Data;
            var partType = GetPartType(Utilities.GetStringAttribute(xml, "partType", ""));
            if (partType == null)
            {
                ServiceProvider.Instance.Game.Designer.DesignerUi.ShowMessage("partType not found! Aborting.");
            }
            try
            {
                PartDataConstructor.Invoke(partData, new object[] { xml, Designer.CraftScript.Data.XmlVersion, partType });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                ServiceProvider.Instance.Game.Designer.DesignerUi.ShowMessage("XML Load error (check console for details). Reverting");
                PartDataConstructor.Invoke(partData, new object[] { backupXml, Designer.CraftScript.Data.XmlVersion, partType });
            }

            UnityEngine.Object.Destroy(part.GameObject);
            CraftBuilder.CallS("CreatePartGameObjects", new PartData[1] { partData }, Designer.CraftScript);
            Designer.SelectPart(partData.PartScript, null, true);
        }
        static PartType GetPartType(string name)
        {
            return (PartType)ServiceProvider.Instance.Game.GetP("PartTypeList").GetP("GetPartType", name);
        }
    }
}