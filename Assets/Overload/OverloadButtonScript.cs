using UnityEngine;
using ModApi.Ui;
using UI.Xml;

namespace WNP78.Overload
{
    class OverloadButtonScript : MonoBehaviour
    {
        GameObject ButtonObject;
        IXmlLayout XmlLayout;
        public void OnLayoutRebuilt(IXmlLayout xmlLayout)
        {
            XmlLayout = xmlLayout;
            ButtonObject = xmlLayout.GetElementById("overload-button").GameObject;
        }
        public void SetButtonEnabled(bool enabled)
        {
            ButtonObject.SetActive(enabled);
        }
        void OverloadButtonClicked()
        {
            OverloadMain.Instance.EditXmlButtonClicked();
        }
    }
}
