using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CharmEdmxTools.EdmxUtils;

namespace CharmEdmxTools.EdmxConfig
{
    [XmlRoot("CharmEdmxToolsConfiguration")]
    public class CharmEdmxConfiguration
    {
        [XmlAttribute("version")]
        public int Version { get; set; }

        public CharmEdmxConfiguration()
        {
            //appSettings = new List<add>();
            SccPocoFixer = new SccPocoFixer();
            EdmMappingConfigurations = new List<edmMappingConfiguration>();
            NamingNavigationProperty = new NamingNavigationProperty();
            //edmMappings = new List<edmMappings>();
        }

        public static CharmEdmxConfiguration Load(string fileName)
        {
            var serializer = new XmlSerializer(typeof(CharmEdmxConfiguration));
            CharmEdmxConfiguration item;
            using (var reader = new StreamReader(fileName))
            {
                item = (CharmEdmxConfiguration)serializer.Deserialize(reader);
            }
            if (item.FillDefaultConfiguration())
                item.Write(fileName);
            return item;
        }

        public void Write(string fileName)
        {
            var serializer = new XmlSerializer(typeof(CharmEdmxConfiguration));
            using (TextWriter writer = new StreamWriter(fileName))
            {
                serializer.Serialize(writer, this);
            }
        }

        //public List<add> appSettings { get; set; }
        public SccPocoFixer SccPocoFixer { get; set; }
        public NamingNavigationProperty NamingNavigationProperty { get; set; }
        public List<edmMappingConfiguration> EdmMappingConfigurations { get; set; }

        //public string GetValue(string key, string defaultValue = "")
        //{
        //    var item = appSettings.FirstOrDefault(it => it.key == key);
        //    if (item != null)
        //        return item.value;
        //    return defaultValue;
        //}
        //public T GetValue<T>(string key, T defaultValue = default(T))
        //{
        //    var item = appSettings.FirstOrDefault(it => it.key == key);
        //    if (item != null)
        //        try
        //        {
        //            var value = item.value;
        //            if (value != "" && (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?)))
        //            {
        //                if (value == "0") value = false.ToString();
        //                else if (value == "1") value = true.ToString();
        //            }
        //            return (T)Convert.ChangeType(value, typeof(T));
        //        }
        //        catch { }
        //    return defaultValue;
        //}
    }

    public class SccPocoFixer
    {
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }

        [XmlAttribute("plugin")]
        public string SccPlugin { get; set; }
    }

    public class add
    {
        [XmlAttribute("key")]
        public string key { get; set; }
        [XmlAttribute("value")]
        public string value { get; set; }
    }

    public class NamingNavigationProperty
    {
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }
        public NamingNavigationPropertyItem ModelOne { get; set; }
        public NamingNavigationPropertyItem ModelMany { get; set; }
        public NamingNavigationPropertyItem ListOne { get; set; }
        public NamingNavigationPropertyItem ListMany { get; set; }
    }

    public class NamingNavigationPropertyItem
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
    }

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

    public class edmMapping
    {
        private string _dbType;

        public edmMapping()
        {
            //conceptualAttributes = new conceptualAttributes();
            ConceptualTrasformations = new List<AttributeTrasformation>();
        }

        public edmMapping(string dbType, params AttributeTrasformation[] trasformations)
            : this()
        {
            DbType = dbType;
            if (trasformations != null)
                ConceptualTrasformations.AddRange(trasformations);
        }

        [XmlAttribute("DbType")]
        public string DbType
        {
            get { return _dbType; }
            set
            {
                _dbType = value;
                DbTypes = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        [NonSerialized]
        [XmlIgnore]
        public string[] DbTypes;


        [XmlAttribute]
        public string Where { get; set; }

        [XmlAttribute]
        public string MinPrecision { get; set; }
        [XmlAttribute]
        public string MaxPrecision { get; set; }
        [XmlAttribute]
        public string MinScale { get; set; }
        [XmlAttribute]
        public string MaxScale { get; set; }

        //public conceptualAttributes conceptualAttributes { get; set; }
        public List<AttributeTrasformation> ConceptualTrasformations { get; set; }
    }

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

    [Serializable]
    public class conceptualAttributes : Dictionary<string, string>, IXmlSerializable
    {
        public string Type { get; set; }
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            //XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            //XmlSerializer valueSerializer = new XmlSerializer(typeof(string));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                if (reader.AttributeCount > 0)
                {
                    for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                    {
                        reader.MoveToAttribute(attInd);
                        Add(reader.Name, reader.Value);
                    }
                }
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (var key in this.Keys)
            {
                writer.WriteAttributeString(key, "", this[key]);
            }
        }
        #endregion
    }

}
