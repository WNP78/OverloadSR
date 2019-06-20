using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Linq;
using UI.Xml;
using WNP78.Overload;

public class UITesterScript : MonoBehaviour
{
    public TextAsset UIXml;
    [TextArea(5,30)]
    public string xml;    
    private void Start()
    {
        XElement partXml = XElement.Parse(xml);
        Debug.Log("Parsed");
        GameObject obj = new GameObject("OverloadXMLEditDialog");
        var script = obj.AddComponent<OverloadXmlEditDialogScript>();
        obj.transform.parent = transform;
        var xmlLayout = obj.AddComponent<XmlLayout>();
        var xmlLayoutController = obj.AddComponent<XmlLayoutController>();
        xmlLayoutController.EventTarget = script;
        xmlLayoutController.OnLayoutRebuilt = (c) => script.OnLayoutRebuilt(c.xmlLayout);
        xmlLayout.Xml = UIXml.text;
        xmlLayout.RebuildLayout(true, true);
    }
    
    
}
