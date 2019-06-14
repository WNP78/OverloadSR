using ModApi;
using ModApi.Design;
using ModApi.Ui;
using System.Xml.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModApi.Craft.Parts;
using ModApi.Mods;
using ModApi.Common;
using Assets.Packages.DevConsole;

namespace WNP78.Overload
{
    public class OverloadMain : GameMod
    {
        public static OverloadMain Instance { get; } = GetModInstance<OverloadMain>();
        public bool Active
        {
            get { return Designer != null; }
        }
        IDesigner Designer;
        public Type CraftBuilder;
        public Type UiUtilities;
        public Type Symmmetry;
        public MethodInfo CreateXmlLayoutFromXml;
        public string DialogXML;
        public string ButtonXML;
        ConstructorInfo PartDataConstructor;
        GameObject OverloadButtonObject;

        readonly Regex Regex1 = new Regex("(.*?)<(\\w+) ([^>/]+)(\\/?>)"); // selects element bodies
        readonly Regex Regex2 = new Regex("\\s*(\\w+=\"[^\"]*\")\\s*"); // separates out attributes

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            CraftBuilder = ReflectionUtils.GetType("Assets.Scripts.Craft.CraftBuilder");
            UiUtilities = ReflectionUtils.GetType("Assets.Scripts.Ui.UiUtilities");
            Symmmetry = ReflectionUtils.GetType("Assets.Scripts.Design.Symmetry");
            DialogXML = ResourceLoader.LoadAsset<TextAsset>("Assets/Dialog.xml").text;
            ButtonXML = ResourceLoader.LoadAsset<TextAsset>("Assets/Button.xml").text;
            CreateXmlLayoutFromXml = UiUtilities.GetMethods(ReflectionUtils.allBindingFlags).First(m => m.Name == "CreateXmlLayoutFromXml" && m.GetParameters().Length == 4);
            PartDataConstructor = typeof(PartData).GetConstructor(new Type[] { typeof(XElement), typeof(int), typeof(PartType) });
            DevConsoleApi.RegisterCommand<string>("LoadDialogXmlFromPath", LoadDialogXmlFromPath);
        }
        void LoadDialogXmlFromPath(string path)
        {
            if (!Active) { return; }
            DialogXML = System.IO.File.ReadAllText(path);
        }
        void OnSceneLoaded(object sender, ModApi.Scenes.Events.SceneEventArgs e)
        {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                Designer = Game.Instance.Designer;
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
        void OnButtonLayoutRebuilt(IXmlLayoutController xmlLayoutController)
        {
            OverloadButtonObject = xmlLayoutController.XmlLayout.GetElementById<RectTransform>("overload-button").gameObject;
            OverloadButtonObject.GetComponent<Button>().onClick.AddListener(EditXmlButtonClicked);
            SelectedPartChanged(null, Designer.SelectedPart);
        }
        void SelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            try
            {
                OverloadButtonObject.SetActive(newPart != null);
            }
            catch (NullReferenceException) { }
        }
        void EditXmlButtonClicked()
        {
            OverloadXmlEditDialogScript.Create(Designer.DesignerUi.Transform, PrettifyXml(GetXML().ToString()), SaveXML);
        }

        IPartScript part;
        XElement backupXml;
        public XElement GetXML()
        {
            part = Designer.SelectedPart;
            var res = part.Data.GenerateXml(part.CraftScript.Transform, false);
            res.SetAttributeValue("activated", part.Data.Activated); // temp fix for activated bug.
            backupXml = XElement.Parse(res.ToString(SaveOptions.DisableFormatting));
            return res;
        }
        public void SaveXML(XElement xml)
        {
            var partData = part.Data;
            var oldConns = partData.PartConnections;
            var oldAPs = partData.AttachPoints;
            var partType = GetPartType(Utilities.GetStringAttribute(xml, "partType", ""));
            if (partType == null)
            {
                Game.Instance.Designer.DesignerUi.ShowMessage("partType not found! Aborting.");
            }
            try
            {
                PartDataConstructor.Invoke(partData, new object[] { xml, Designer.CraftScript.Data.XmlVersion, partType });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Game.Instance.Designer.DesignerUi.ShowMessage("XML Load error (check console for details). Reverting");
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
        PartType GetPartType(string name)
        {
            return (PartType)Game.Instance.GetP("PartTypes").Call("GetPartType", name);
        }
        string PrettifyXml(string xmlIn)
        {
            return Regex1.Replace(xmlIn, match =>
            {
                StringBuilder s = new StringBuilder();
                s.Append(match.Groups[1].Value);
                s.Append('<');
                s.Append(match.Groups[2].Value);
                s.Append('\n');
                s.Append(Regex2.Replace(match.Groups[3].Value, match.Groups[1].Value + "  $1\n"));
                s.Append(match.Groups[1].Value);
                s.Append(match.Groups[4].Value);
                return s.ToString();
            }).Replace("\r", "");
        }
    }
}