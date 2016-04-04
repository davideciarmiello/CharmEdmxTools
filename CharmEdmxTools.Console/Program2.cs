using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EdmxCustomizer.Console.EdmxConfiguration;
using HP.EdmxCustomizer.EdmxUtils;
using CustomConfigManager = HP.EdmxCustomizer.EdmxUtils.CustomConfigManager;

namespace EdmxCustomizer.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var newCfg = new EdmxCustomizer.Console.EdmxConfiguration.Configuration();
            newCfg.appSettings.Add(new add() { key = "prova", value = "valore" });
            var map = new edmMapping() { DBType = "dbtype", Where = "Precision < 18", MaxPrecision = "8"};
            map.conceptualAttributes["Type2"] = "boolNet";
            map.conceptualAttributes.Type = "prova";
            var mapCfg = new edmMappingConfiguration();
            mapCfg.ProviderName = "Oracle";
            mapCfg.edmMappings.Add(map);
            newCfg.edmMappingConfigurations.Add(mapCfg);
            /*var mapConfig = new edmMappings() {ProviderName = "Oracle"};
            mapConfig.Add(map);
            newCfg.edmMappings.Add(mapConfig);*/


            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            using (TextWriter writer = new StreamWriter(@"C:\Davide\Xml.xml"))
            {
                serializer.Serialize(writer, newCfg);
            }

            var cfg = new CustomConfigManager();
            cfg.LoadConfigsFromFileIfExists(@"C:\Davide\Progetti\ConsoleApplication1\EdmxCustomizer.config");


            return;
            //var mgr = new EdmxManager(@"R:\Davide\GrinDbContext.edmx");
            var mgr = new EdmxManager(@"C:\Davide\Progetti\ConsoleApplication1\ConsoleApplication1\GrinModel.edmx", null, null);
            //mgr.Avvia();
            mgr.FixPropertiesAttributes();
            mgr.ClearEdmxPreservingKeyFields();
            mgr.FixConceptualModelNames();
            mgr.FixTabelleECampiEliminati();
            mgr.Salva();
            if (mgr.StorageTypeNotManaged.Count > 0)
                System.Console.WriteLine("Tipi non gestiti:" + string.Join(",", mgr.StorageTypeNotManaged));
            System.Console.WriteLine("Premere un tasto per uscire.");
            System.Console.ReadKey();
        }
    }
}
