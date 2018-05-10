using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    [XmlType("ManualOperation")]
    [XmlRoot("ManualOperation")]
    public class ManualOperation
    {
        [XmlAttribute]
        public ManualOperationType Type { get; set; }

        [XmlAttribute]
        public string TableName { get; set; }
        
        [XmlAttribute]
        public string FieldName { get; set; }

        [XmlAttribute]
        public string AssociationName { get; set; }

        [XmlAttribute]
        public string AttributeName { get; set; }
        [XmlAttribute]
        public string AttributeValue { get; set; }
    }
}