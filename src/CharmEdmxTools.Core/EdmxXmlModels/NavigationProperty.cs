using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class NavigationProperty : BaseItem
    {
        public NavigationProperty(XElement node)
            : base(node)
        {
        }
        public string Relationship { get { return XNode.GetAttribute("Relationship"); } }

        public string FromRole { get { return XNode.GetAttribute("FromRole"); } }
        public string ToRole { get { return XNode.GetAttribute("ToRole"); } }
    }
}