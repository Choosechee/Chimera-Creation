using System;
using System.Xml;
using Verse;

namespace AnomalyAllies.Misc
{
    /// <summary>
    /// Attempts to add a Name attribute to a node. If the node already has a Name attribute, 
    /// it replaces all ParentName attributes that have the value attempted to be added with 
    /// the value that was found.
    /// </summary>
    public class PatchOperationAddNameOrReplaceAllUses : PatchOperationPathed
    {
        protected const string name = "Name";
        protected const string parentName = "ParentName";

        protected const string multipleMatchesError = "Found more than one node matching xpath";

        protected string value;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            XmlNodeList nodeList = xml.SelectNodes(xpath);
            if (nodeList.Count > 1)
                throw new Exception(multipleMatchesError);
            else if (nodeList.Count == 0)
                return false;

            XmlNode selectedNode = nodeList[0];
            XmlAttribute nameAttribute = selectedNode.Attributes[name];

            bool result = false;
            if (nameAttribute is null)
            {
                XmlAttribute newNameAttribute = xml.CreateAttribute(name);
                newNameAttribute.Value = value;
                selectedNode.Attributes.Append(newNameAttribute);

                result = true;
            }
            else
            {
                string foundValue = nameAttribute.Value;
                string newXPath = $"Defs/{selectedNode.Name}[@{name}=\"{value}\"]";

                foreach (XmlNode node in xml.SelectNodes(newXPath))
                {
                    node.Attributes[name].Value = foundValue;
                    result = true;
                }
            }

            return result;
        }
    }
}
