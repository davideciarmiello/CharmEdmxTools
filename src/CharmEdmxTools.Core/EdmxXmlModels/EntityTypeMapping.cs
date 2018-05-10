using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class EntityTypeMapping : BaseItem
    {
        public EntityTypeMapping(XElement node)
            : base(node)
        {
        }
        public string TypeName { get { return XNode.GetAttribute("TypeName"); } }

    }
}