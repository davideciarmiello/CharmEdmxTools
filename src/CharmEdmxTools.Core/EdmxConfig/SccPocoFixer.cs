using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    public class SccPocoFixer
    {
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }

        [XmlAttribute("plugin")]
        public string SccPlugin { get; set; }
    }
}