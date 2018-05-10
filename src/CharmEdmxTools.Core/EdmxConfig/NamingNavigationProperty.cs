using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    public class NamingNavigationProperty
    {
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }
        public NamingNavigationPropertyItem ModelOne { get; set; }
        public NamingNavigationPropertyItem ModelMany { get; set; }
        public NamingNavigationPropertyItem ModelOneParent { get; set; }
        public NamingNavigationPropertyItem ListOne { get; set; }
        public NamingNavigationPropertyItem ListMany { get; set; }
        public NamingNavigationPropertyItem ListOneChilds { get; set; }
    }
}