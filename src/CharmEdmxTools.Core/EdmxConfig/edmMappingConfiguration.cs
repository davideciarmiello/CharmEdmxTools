using System.Collections.Generic;
using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    public class edmMappingConfiguration
    {
        public edmMappingConfiguration()
        {
            edmMappings = new List<edmMapping>();
        }
        [XmlAttribute]
        public string ProviderName { get; set; }
        public List<edmMapping> edmMappings { get; set; }
    }
}