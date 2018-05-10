using System.Linq;
using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class EntitySetMapping : BaseItem
    {
        public EntitySetMapping(XElement node)
            : base(node)
        {
        }

        private string _conceptualTypeName;
        public string ConceptualTypeName
        {
            get { return _conceptualTypeName ?? (_conceptualTypeName = XNode.Elements().First(x => x.Name.LocalName == "EntityTypeMapping").GetAttribute("TypeName")); }
        }

        private string _storeEntitySet;
        public string StoreEntitySet
        {
            get { return _storeEntitySet ?? (_storeEntitySet = XNode.Descendants().First(x => x.Name.LocalName == "MappingFragment").GetAttribute("StoreEntitySet")); }
        }

    }
}