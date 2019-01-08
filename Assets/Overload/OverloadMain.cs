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
        static IDesigner Designer;
        static Type CraftBuilder;
        static Type UiUtilities;
        static Type Symmmetry;
        static string DialogXML;
        static string ButtonXML;
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

                var layout = flyout.Transform.GetComponentInChildren<IXmlLayout>();
                /*var el = XElement.Parse(layout.Xml);
                var root = el.Descendants().First(x => Utilities.GetStringAttribute(x, "id", "") == "content-root");
                var btEl = XElement.Parse("<Button id=\"overload-button\" class=\"btn\" width=\"150\"><TextMeshPro text=\"EDIT XML\" /></Button>");
                root.AddFirst(btEl);
                layout.Xml = el.ToString();
                layout.RebuildLayout(false, true);
                OverloadButtonObject = layout.GetElementById<RectTransform>("overload-button").gameObject;
                */
                var root = layout.GetElementById<RectTransform>("content-root");
                GameObject obj = (GameObject)UiUtilities.CallS("CreateUiGameObject", "OverloadButton", root);
                obj.AddComponent<LayoutElement>().minHeight = 30;
                obj.transform.SetAsFirstSibling();

                UiUtilities.GetMethods(ReflectionUtils.allBindingFlags).First(m => m.Name == "CreateXmlLayoutFromXml" && m.GetParameters().Length == 4).Invoke(null, new object[] { ButtonXML, obj, null, (Action<IXmlLayoutController>)OnButtonLayoutRebuilt });
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
        static void PartPropertiesOpened(IFlyout flyout)
        {
            Debug.Log("opened: " + flyout.Transform);
        }
        static void SelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            OverloadButtonObject?.SetActive(newPart != null);
        }
        static void EditXmlButtonClicked()
        {
            Debug.Log("Edit Xml!");
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

            var oldSlice = part.SymmetrySlice;

            UnityEngine.Object.Destroy(part.GameObject);
            CraftBuilder.CallS("CreatePartGameObjects", new PartData[1] { partData }, Designer.CraftScript);
            Designer.SelectPart(partData.PartScript, null, true);

            partData.PartScript.SymmetrySlice = oldSlice;
            Symmmetry.CallS("SynchronizeParts", partData.PartScript, true);
        }
        static PartType GetPartType(string name)
        {
            return (PartType)ServiceProvider.Instance.Game.GetP("PartTypeList").GetP("GetPartType", name);
        }
    }
}