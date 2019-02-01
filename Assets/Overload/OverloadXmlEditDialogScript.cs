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

namespace WNP78.Overload
{
    public class OverloadXmlEditDialogScript : DialogScript
    {
        string xml;
        Action<XElement> onSave;
        TMP_InputField inputField;

        public static OverloadXmlEditDialogScript Create(Transform parent, string xml, Action<XElement> onSave)
        {
            OverloadXmlEditDialogScript dialog = (OverloadXmlEditDialogScript)OverloadMain.UiUtilities.GetMethod("CreateDialog", ReflectionUtils.allBindingFlags).MakeGenericMethod(new Type[] { typeof(OverloadXmlEditDialogScript) }).Invoke(null, new object[] { parent, true });
            dialog.xml = xml;
            dialog.onSave = onSave;
            Action<IXmlLayoutController> action = x => dialog.OnLayoutRebuilt(x.XmlLayout);
            OverloadMain.CreateXmlLayoutFromXml.Invoke(null, new object[] { OverloadMain.DialogXML, dialog.gameObject, dialog, action });
            return dialog;
        }

        void OnLayoutRebuilt(IXmlLayout layout)
        {
            inputField = layout.GetElementById<TMP_InputField>("xml-input");
            inputField.richText = false;
            inputField.text = xml;
            inputField.GetComponentInChildren<TMP_Text>().SetText(inputField.text);
            inputField.onFocusSelectAll = false;
            inputField.scrollSensitivity = 5f;

            inputField.ForceLabelUpdate();
            StartCoroutine(LayoutRebuilt());
        }
        IEnumerator LayoutRebuilt()
        {
            yield return null;
            yield return null;
            inputField.ForceLabelUpdate();
        }
        void OnSaveButtonClicked()
        {
            onSave?.Invoke(XElement.Parse(inputField.text));
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
