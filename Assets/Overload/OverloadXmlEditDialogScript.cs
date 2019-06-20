using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using ModApi;
using ModApi.Ui;
using TMPro;
using ModApi.Common;
using Assets.Scripts.Ui;

namespace WNP78.Overload
{
    public class OverloadXmlEditDialogScript : DialogScript
    {
        string xml;
        XElement _xml;
        Action<XElement> onSave;
        //TMP_InputField inputField;
        IXmlLayout layout;
        Dictionary<string, XElement> elements;
        GameObject template;
        public List<OverloadXmlEditRowScript> rows = new List<OverloadXmlEditRowScript>();
        public void ReloadFromFile(string path)
        {
            string text = System.IO.File.ReadAllText(path);
            layout.Xml = text;
            layout.RebuildLayout(true, true);
        }
        public void ReloadFromDefaultPath()
        {
            ReloadFromFile(@"E:\UnitySRProjects\OverloadSR\Assets\Overload\Dialog.xml");
        }
        public static OverloadXmlEditDialogScript Create(Transform parent, XElement xml, Action<XElement> onSave)
        {
            //OverloadXmlEditDialogScript dialog = (OverloadXmlEditDialogScript)OverloadMain.Instance.UiUtilities.GetMethod("CreateDialog", ReflectionUtils.allBindingFlags).MakeGenericMethod(new Type[] { typeof(OverloadXmlEditDialogScript) }).Invoke(null, new object[] { parent, true });
            /*var dialog = Game.Instance.UserInterface.CreateDialog<OverloadXmlEditDialogScript>(parent, true);
            dialog.xml = xml;
            dialog.onSave = onSave;
            Action<IXmlLayoutController> action = x => dialog.OnLayoutRebuilt(x.XmlLayout);
            Game.Instance.UserInterface.BuildUserInterfaceFromXml(OverloadMain.Instance.DialogXML, "overload_dialog", dialog, action);*/

            //OverloadMain.Instance.CreateXmlLayoutFromXml.Invoke(null, new object[] { OverloadMain.Instance.DialogXML, dialog.gameObject, dialog, action });
            return Game.Instance.UserInterface.CreateDialog<OverloadXmlEditDialogScript>("Overload/Dialog", parent, (d, c) => d.OnLayoutRebuilt(c.XmlLayout), s =>
            {
                s._xml = xml;
                s.onSave = onSave;
            });
        }

        public void OnLayoutRebuilt(IXmlLayout layout)
        {
            this.layout = layout;
            template = layout.GetElementById("row-template").GameObject;
            var spinner = layout.GetElementById<SpinnerScript>("overload-spinner");
            elements = new Dictionary<string, XElement>();
            spinner.Values.Clear();
            foreach (var el in _xml.DescendantsAndSelf())
            {
                elements.Add(el.Name.LocalName, el);
                spinner.Values.Add(el.Name.LocalName);
            }
            spinner.Value = _xml.Name.LocalName;
            spinner.OnValueChanged += OnSpinnerChange;
            rows.Clear();
            OnSpinnerChange(_xml.Name.LocalName);
        }

        void OnSpinnerChange(string newValue)
        {
            var currentElement = elements[newValue];
            int i = 0;
            foreach (var row in rows)
            {
                row.Deactivate();
            }
            foreach (var attr in currentElement.Attributes())
            {
                Debug.Log(attr.Name.LocalName);
                if (i < rows.Count)
                {
                    rows[i].Initialise(attr, this);
                }
                else
                {
                    var row = Instantiate(template);
                    row.transform.SetParent(template.transform.parent);
                    var script = row.AddComponent<OverloadXmlEditRowScript>();
                    script.Initialise(attr, this);
                    rows.Add(script);
                }
                i++;
            }
        }

        void OnSaveButtonClicked()
        {
            onSave?.Invoke(_xml);
            Close();
        }
        void OnCancelButtonClicked()
        {
            Close();
        }

        public override void Close()
        {
            base.Close();
            Destroy(gameObject);
        }
    }
}
