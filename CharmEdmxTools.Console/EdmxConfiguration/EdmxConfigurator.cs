using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace EdmxCustomizer.Console.EdmxConfiguration
{


    using System.Collections.Concurrent;
    using System.Configuration;

    public class CustomConfigManager
    {

        public void WriteConfigsToFile(string configFilePath, ICollection<KeyValuePair<string, string>> keysAndValues)
        {
            if (!System.IO.File.Exists(configFilePath))
            {
                System.IO.File.WriteAllText(configFilePath, @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
  </appSettings>
</configuration>");
            }
            // Either load up the existing file or create a blank file
            var config = ConfigurationManager.OpenMappedExeConfiguration(
                new ExeConfigurationFileMap { ExeConfigFilename = configFilePath },
                ConfigurationUserLevel.None);
            foreach (var item in keysAndValues)
            {
                config.AppSettings.Settings.Add(item.Key, item.Value);
            }
            config.Save();
        }

        private Dictionary<string, string> settings = new Dictionary<string, string>();
        public void LoadConfigsFromFileIfExists(params string[] configFilePaths)
        {
            foreach (var configFilePath in configFilePaths)
            {
                if (!System.IO.File.Exists(configFilePath))
                    continue;
                var config = ConfigurationManager.OpenMappedExeConfiguration(
                    new ExeConfigurationFileMap { ExeConfigFilename = configFilePath },
                    ConfigurationUserLevel.None);
                foreach (KeyValueConfigurationElement item in config.AppSettings.Settings)
                {
                    if (settings.ContainsKey(item.Key))
                        settings[item.Key] = item.Value;
                    else
                        settings.Add(item.Key, item.Value);
                }
            }
        }

        public string GetValue(string key, string defaultValue = "")
        {
            string value;
            if (settings.TryGetValue(key, out value))
                return value;
            return defaultValue;
        }
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            string value;
            if (settings.TryGetValue(key, out value))
                try
                {
                    if (value != "" && (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?)))
                    {
                        if (value == "0") value = false.ToString();
                        else if (value == "1") value = true.ToString();
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { }
            return defaultValue;
        }
    }

    [XmlRoot("configuration")]
    public class Configuration
    {
        public Configuration()
        {
            appSettings = new List<add>();
            edmMappingConfigurations = new List<edmMappingConfiguration>();
            //edmMappings = new List<edmMappings>();
        }

        public List<add> appSettings { get; set; }

        public List<edmMappingConfiguration> edmMappingConfigurations { get; set; }

        //public List<edmMappings> edmMappings { get; set; }
    }

    public class add
    {
        [XmlAttribute("key")]
        public string key { get; set; }
        [XmlAttribute("value")]
        public string value { get; set; }
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
        public edmMapping()
        {
            conceptualAttributes = new conceptualAttributes();
        }
        [XmlAttribute("DBType")]
        public string DBType { get; set; }
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

        public conceptualAttributes conceptualAttributes { get; set; }
    }

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
