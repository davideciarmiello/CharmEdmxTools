using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    public class NamingNavigationPropertyItem
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
    }
}