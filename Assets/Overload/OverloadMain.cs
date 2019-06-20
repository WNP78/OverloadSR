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
            get { return Game.Instance.Designer != null; }
        }
        //IGame.Instance.Designer Game.Instance.Designer;
        public Type CraftBuilder;
        public Type UiUtilities;
        public Type Symmmetry;
        ConstructorInfo PartDataConstructor;
        OverloadButtonScript buttonScript;

        readonly Regex Regex1 = new Regex("(.*?)<(\\w+) ([^>]+)(\\/?>)"); // selects element bodies
        readonly Regex Regex2 = new Regex("\\s*(\\w+=\"[^\"]*\")\\s*/?"); // separates out attributes

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            CraftBuilder = ReflectionUtils.GetType("Assets.Scripts.Craft.CraftBuilder");
            UiUtilities = ReflectionUtils.GetType("Assets.Scripts.Ui.UiUtilities");
            Symmmetry = ReflectionUtils.GetType("Assets.Scripts.Design.Symmetry");
            PartDataConstructor = typeof(PartData).GetConstructor(new Type[] { typeof(XElement), typeof(int), typeof(PartType) });
        }
        void OnSceneLoaded(object sender, ModApi.Scenes.Events.SceneEventArgs e)
        {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                var flyout = Game.Instance.Designer.DesignerUi.Flyouts.PartProperties;
                Game.Instance.Designer.SelectedPartChanged += SelectedPartChanged;

                var layout = flyout.Transform.GetComponentInChildren<IXmlLayout>();
                var root = layout.GetElementById<RectTransform>("content-root");
                
                buttonScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<OverloadButtonScript>("Overload/Button", (s, c) =>
                {
                    s.OnLayoutRebuilt(c.XmlLayout);
                    SelectedPartChanged(null, Game.Instance.Designer.SelectedPart);
                }, root);
                buttonScript.gameObject.AddComponent<LayoutElement>().minHeight = 30;
                buttonScript.transform.SetAsFirstSibling();
            }
        }
        void SelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            try
            {
                buttonScript.SetButtonEnabled(newPart != null);
            }
            catch (NullReferenceException) { }
        }
        public void EditXmlButtonClicked()
        {
            OverloadXmlEditDialogScript.Create(Game.Instance.Designer.DesignerUi.Transform, GetXML(), SaveXML);
        }

        IPartScript part;
        XElement backupXml;
        public XElement GetXML()
        {
            part = Game.Instance.Designer.SelectedPart;
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
                PartDataConstructor.Invoke(partData, new object[] { xml, Game.Instance.Designer.CraftScript.Data.XmlVersion, partType });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Game.Instance.Designer.DesignerUi.ShowMessage("XML Load error (check console for details). Reverting");
                PartDataConstructor.Invoke(partData, new object[] { backupXml, Game.Instance.Designer.CraftScript.Data.XmlVersion, partType });
            }
            partData.SetP("PartConnections", oldConns);
            partData.SetP("AttachPoints", oldAPs); // because private setter

            var oldSlice = part.SymmetrySlice;

            UnityEngine.Object.Destroy(part.GameObject);
            CraftBuilder.CallS("CreatePartGameObjects", new PartData[1] { partData }, Game.Instance.Designer.CraftScript);
            Game.Instance.Designer.SelectPart(partData.PartScript, null, true);

            partData.PartScript.SymmetrySlice = oldSlice;

            Symmmetry.CallS("SynchronizeParts", partData.PartScript, true);

            Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
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
                var body = match.Groups[3].Value;
                if (body[body.Length - 1] == '/')
                {
                    s.Append("/");
                }
                s.Append(match.Groups[4].Value);
                return s.ToString();
            }).Replace("\r", "");
        }
    }
}