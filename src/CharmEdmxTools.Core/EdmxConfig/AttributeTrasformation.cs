using System;
using System.Xml.Serialization;

namespace CharmEdmxTools.Core.EdmxConfig
{
    [XmlType("add")]
    [XmlRoot("add")]
    public class AttributeTrasformation
    {
        public AttributeTrasformation()
        {

        }

        public AttributeTrasformation(string name, string value)
        {
            Name = name;
            Value = value;
        }

        private string _name;

        [NonSerialized]
        [XmlIgnore]
        public string[] NameList;

        private string _value;

        [XmlAttribute("AttributeName")]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NameList = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        [XmlAttribute]
        public string Value
        {
            get { return _value ?? (string.IsNullOrWhiteSpace(ValueStorageAttributeName) && !ValueFromStorageAttribute ? "" : null); }
            set { _value = value; }
        }

        [XmlAttribute]
        public string ValueStorageAttributeName { get; set; }

        [XmlIgnore]
        public bool ValueFromStorageAttribute { get; set; }

        [XmlAttribute("ValueFromStorageAttribute")]
        public string ValueFromStorageAttributeString
        {
            get { return ValueFromStorageAttribute ? true.ToString() : null; }
            set { ValueFromStorageAttribute = !string.IsNullOrEmpty(value) && Convert.ToBoolean(value); }
        }
    }
}