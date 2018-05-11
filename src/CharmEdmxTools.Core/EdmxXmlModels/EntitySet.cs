using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class EntitySet : BaseItem
    {
        public EntitySet(XElement node)
            : base(node)
        {
        }

        public string EntityType
        {
            get { return XNode.GetAttribute("EntityType"); }
            set
            {
                var attr = XNode.Attribute("EntityType");
                if (attr != null) attr.Value = value;
            }
        }

    }
}