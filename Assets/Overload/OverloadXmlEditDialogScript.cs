using System;
using System.Reflection;
using System.Collections.Generic;
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
        XElement xElement;
        Action<XElement> onSave;
        TMP_InputField inputField;

        public static OverloadXmlEditDialogScript Create(Transform parent, XElement xElement, Action<XElement> onSave)
        {
            OverloadXmlEditDialogScript dialog = (OverloadXmlEditDialogScript)OverloadMain.UiUtilities.GetMethod("CreateDialog", ReflectionUtils.allBindingFlags).MakeGenericMethod(new Type[] { typeof(OverloadXmlEditDialogScript) }).Invoke(null, new object[] { parent, true });
            dialog.xElement = xElement;
            dialog.onSave = onSave;
            Action<IXmlLayoutController> action = x => dialog.OnLayoutRebuilt(x.XmlLayout);
            OverloadMain.CreateXmlLayoutFromXml.Invoke(null, new object[] { OverloadMain.DialogXML, dialog.gameObject, dialog, action });
            return dialog;
        }

        void OnLayoutRebuilt(IXmlLayout layout)
        {
            inputField = layout.GetElementById<TMP_InputField>("xml-input");
            inputField.richText = false;
            inputField.text = xElement.ToString();
            inputField.GetComponentInChildren<TMP_Text>().SetText(inputField.text);
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
    }
}
