using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class AssociationSet : BaseItem
    {
        public AssociationSet(XElement node)
            : base(node)
        {
        }
        public string Association { get { return XNode.GetAttribute("Association"); } }

    }
}