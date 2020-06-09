using System;
using System.Collections;
using UnityEngine;
using UI.Xml;
using TMPro;
using System.Xml.Linq;

namespace WNP78.Overload
{
    public class OverloadXmlEditRowScript : MonoBehaviour
    {
        XAttribute xAttribute;
        TMP_InputField nameInput;
        TMP_InputField valueInput;
        OverloadXmlEditDialogScript dialogScript;
        private void Awake()
        {
            var xmlElement = GetComponent<XmlElement>();
            nameInput = xmlElement.GetElementByInternalId<TMP_InputField>("name-input");
            valueInput = xmlElement.GetElementByInternalId<TMP_InputField>("value-input");
            xmlElement.GetElementByInternalId<XmlLayoutButtonComponent>("delete-button").onClick.AddListener(OnDeleteClicked);
        }
        private void Start()
        {
            valueInput.onValueChanged.AddListener(OnValueChanged);
            foreach (var f in GetComponentsInChildren<TMP_InputField>())
            {
                f.gameObject.AddComponent<US.UI.InputFieldScrollFixer>();
            }

            StartCoroutine(FixThingies());
        }
        void OnNameChanged(string name)
        {
            xAttribute.Parent.SetAttributeValue(name, xAttribute.Value);
            var newAttr = xAttribute.Parent.Attribute(name);
            xAttribute.Remove();
            xAttribute = newAttr;
        }
        void OnValueChanged(string value)
        {
            xAttribute.Value = value;
        }
        void OnDeleteClicked()
        {
            xAttribute.Remove();
            dialogScript.rows.Remove(this);
            Destroy(gameObject);
        }
        public void Initialise(XAttribute attribute, OverloadXmlEditDialogScript parent)
        {
            dialogScript = parent;
            gameObject.SetActive(true);
            xAttribute = attribute;
            nameInput.text = attribute.Name.LocalName;
            valueInput.text = attribute.Value;
        }
        public void Deactivate()
        {
            xAttribute = null;
            gameObject.SetActive(false);
        }
        IEnumerator FixThingies()
        {
            nameInput.SetTextWithoutNotify("");
            valueInput.SetTextWithoutNotify("");
            yield return null;
            nameInput.text = xAttribute.Name.LocalName;
            valueInput.text = xAttribute.Value;
        }
    }
}
