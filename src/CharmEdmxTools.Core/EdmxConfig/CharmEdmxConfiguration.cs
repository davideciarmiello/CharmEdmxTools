using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using CharmEdmxTools.Core.ExtensionsMethods;

namespace CharmEdmxTools.Core.EdmxConfig
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
            ManualOperations = new List<ManualOperation>();
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
        public List<ManualOperation> ManualOperations { get; set; }
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
}
