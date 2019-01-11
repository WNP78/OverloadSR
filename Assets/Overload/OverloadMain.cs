using ModApi;
using ModApi.Design;
using ModApi.Ui;
using System.Xml.Linq;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModApi.Craft.Parts;

namespace WNP78.Overload
{
    public static class OverloadMain
    {
        public static bool Active
        {
            get { return Designer != null; }
        }
        static IDesigner Designer;
        public static Type CraftBuilder;
        public static Type UiUtilities;
        public static Type Symmmetry;
        public static MethodInfo CreateXmlLayoutFromXml;
        public static string DialogXML;
        public static string ButtonXML;
        static ConstructorInfo PartDataConstructor;
        static GameObject OverloadButtonObject;
        public static void Init()
        {
            ServiceProvider.Instance.Game.SceneManager.SceneLoaded += OnSceneLoaded;
            CraftBuilder = ReflectionUtils.GetType("Assets.Scripts.Craft.CraftBuilder");
            UiUtilities = ReflectionUtils.GetType("Assets.Scripts.Ui.UiUtilities");
            Symmmetry = ReflectionUtils.GetType("Assets.Scripts.Design.Symmetry");
            DialogXML = ServiceProvider.Instance.ResourceLoader.LoadAsset<TextAsset>("Assets/Dialog.xml").text;
            ButtonXML = ServiceProvider.Instance.ResourceLoader.LoadAsset<TextAsset>("Assets/Button.xml").text;
            CreateXmlLayoutFromXml = UiUtilities.GetMethods(ReflectionUtils.allBindingFlags).First(m => m.Name == "CreateXmlLayoutFromXml" && m.GetParameters().Length == 4);
            PartDataConstructor = typeof(PartData).GetConstructor(new Type[] { typeof(XElement), typeof(int), typeof(PartType) });
            ServiceProvider.Instance.DevConsole.RegisterCommand<string>("LoadDialogXmlFromPath", LoadDialogXmlFromPath);
            
        }
        static void LoadDialogXmlFromPath(string path)
        {
            if (!Active) { return; }
            DialogXML = System.IO.File.ReadAllText(path);
        }
        static void OnSceneLoaded(object sender, ModApi.Scenes.Events.SceneEventArgs e)
        {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                Designer = ServiceProvider.Instance.Game.Designer;
                var flyout = Designer.DesignerUi.Flyouts.PartProperties;
                Designer.SelectedPartChanged += SelectedPartChanged;

                var layout = flyout.Transform.GetComponentInChildren<IXmlLayout>();
                var root = layout.GetElementById<RectTransform>("content-root");
                GameObject obj = (GameObject)UiUtilities.CallS("CreateUiGameObject", "OverloadButton", root);
                obj.AddComponent<LayoutElement>().minHeight = 30;
                obj.transform.SetAsFirstSibling();

                CreateXmlLayoutFromXml.Invoke(null, new object[] { ButtonXML, obj, null, (Action<IXmlLayoutController>)OnButtonLayoutRebuilt });
            }
            else
            {
                Designer = null;
            }
        }
        static void OnButtonLayoutRebuilt(IXmlLayoutController xmlLayoutController)
        {
            OverloadButtonObject = xmlLayoutController.XmlLayout.GetElementById<RectTransform>("overload-button").gameObject;
            OverloadButtonObject.GetComponent<Button>().onClick.AddListener(EditXmlButtonClicked);
            SelectedPartChanged(null, Designer.SelectedPart);
        }
        static void SelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            try
            {
                OverloadButtonObject.SetActive(newPart != null);
            }
            catch (NullReferenceException) { }
        }
        static void EditXmlButtonClicked()
        {
            OverloadXmlEditDialogScript.Create(Designer.DesignerUi.Transform, GetXML(), SaveXML);
        }

        static IPartScript part;
        static XElement backupXml;
        public static XElement GetXML()
        {
            part = Designer.SelectedPart;
            var res = part.Data.GenerateXml(part.CraftScript.Transform, false);
            backupXml = XElement.Parse(res.ToString(SaveOptions.DisableFormatting));
            return res;
        }
        public static void SaveXML(XElement xml)
        {
            var partData = part.Data;
            var oldConns = partData.PartConnections;
            var oldAPs = partData.AttachPoints;
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
            partData.SetP("PartConnections", oldConns);
            partData.SetP("AttachPoints", oldAPs); // because private setter

            var oldSlice = part.SymmetrySlice;

            UnityEngine.Object.Destroy(part.GameObject);
            CraftBuilder.CallS("CreatePartGameObjects", new PartData[1] { partData }, Designer.CraftScript);
            Designer.SelectPart(partData.PartScript, null, true);

            partData.PartScript.SymmetrySlice = oldSlice;

            Symmmetry.CallS("SynchronizeParts", partData.PartScript, true);

            Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
        static PartType GetPartType(string name)
        {
            return (PartType)ServiceProvider.Instance.Game.GetP("PartTypes").Call("GetPartType", name);
        }
    }
}