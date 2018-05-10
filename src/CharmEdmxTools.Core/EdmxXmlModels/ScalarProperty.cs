using System.Xml.Linq;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxXmlModels
{
    public class ScalarProperty : BaseItem
    {
        public ScalarProperty(XElement node)
            : base(node)
        {
        }
        public string ColumnName { get { return XNode.GetAttribute("ColumnName"); } }
    }
}