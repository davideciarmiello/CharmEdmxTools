using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class EntityType : BaseItem
    {
        public EntityType(XElement node)
            : base(node)
        {
            Fill();
        }
        private List<Property> _property;
        private List<NavigationProperty> _navigationProperty;
        private void Fill()
        {
            _property = Descendants<Property>().ToList();
            _navigationProperty = Descendants<NavigationProperty>().ToList();
        }
        public IEnumerable<Property> Properties { get { return _property.NotDeleted(); } }
        public IEnumerable<NavigationProperty> NavigationProperties { get { return _navigationProperty.NotDeleted(); } }

        public XComment WarningMessage
        {
            get
            {
                var comm = IsDeleted ? null : (XNode.PreviousNode as XComment);
                if (comm == null)
                    return null;
                var isMine = comm.Value.Contains($".{Name}\'") || comm.Value.Contains($"\'{Name}\'");
                return isMine ? comm : null;
            }
        }
    }
}