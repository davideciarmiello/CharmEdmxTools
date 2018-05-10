using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class End : BaseItem
    {
        public End(XElement node)
            : base(node)
        {
        }

        public virtual string Role
        {
            get { return XNode.GetAttribute("Role"); }
        }
        public virtual string Multiplicity
        {
            get { return XNode.GetAttribute("Multiplicity"); }
            set { var attr = XNode.Attribute("Multiplicity"); if (attr != null) { attr.Value = value; } }
        }

    }
}