using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using AppCodeShared;
using CharmEdmxTools.Core.EdmxConfig;
using CharmEdmxTools.Core.Manager;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace CharmEdmxTools.Console
{
    class Program
    {
        //static void InvokeFull(EdmxManager mgr)
        //{
        //    mgr.FieldsManualOperations();
        //    mgr.FixTabelleECampiEliminati();
        //    if (!mgr.AssociationContainsDifferentTypes())
        //        mgr.FixTabelleNonPresentiInConceptual();
        //    mgr.FixAssociations();
        //    mgr.FixPropertiesAttributes();
        //    mgr.FixConceptualModelNames();
        //}
        static void InvokeFull(EdmxManager mgr)
        {
            mgr.ExecAllFixs();
        }

        static void Main(string[] args)
        {
            var cfgFileName =
                @"C:\tfs\GRIN\dev-rin1\src\Gse.Grin.Platform.Solution\Gse.Grin.Platform.Solution.sln.CharmEdmxTools";
            var edmxFileName = cfgFileName.Replace("Gse.Grin.Platform.Solution.sln.CharmEdmxTools",
                @"Gse.Grin.DataBaseContext.EF\GrinDbContext.edmx");
            var cfg = CharmEdmxConfiguration.Load(cfgFileName);
            //var xdoc = XDocument.Load(edmxFileName);
            //var x = new EdmxContainerNew(xdoc);
            
            var sw = Stopwatch.StartNew();

            //var mgr = new EdmxManager(edmxFileName, System.Console.WriteLine, cfg);
            sw = Stopwatch.StartNew();
            var mgrnew = new EdmxManager(edmxFileName, System.Console.WriteLine, cfg);
            InvokeFull(mgrnew);
            sw.Stop();
            //13 secondi
            System.Console.WriteLine(sw.Elapsed);

            //TestTfs();
            return;
            //var newCfg = new CharmEdmxConfiguration();
            ////newCfg.appSettings.Add(new add() { key = "prova", value = "valore" });
            //var map = new edmMapping() { DbType = "dbtype", Where = "Precision < 18", MaxPrecision = "8" };
            //map.conceptualAttributes["Type2"] = "boolNet";
            //map.conceptualAttributes.Type = "prova";
            //var mapCfg = new edmMappingConfiguration();
            //mapCfg.ProviderName = "Oracle";
            //mapCfg.edmMappings.Add(map);
            //newCfg.EdmMappingConfigurations.Add(mapCfg);
            //*var mapConfig = new edmMappings() {ProviderName = "Oracle"};
            //mapConfig.Add(map);
            //newCfg.edmMappings.Add(mapConfig);*/


            //XmlSerializer serializer = new XmlSerializer(typeof(CharmEdmxConfiguration));
            //using (TextWriter writer = new StreamWriter(@"C:\Davide\Xml.xml"))
            //{
            //    serializer.Serialize(writer, newCfg);
            //}

            ////var cfg = new CustomConfigManager();
            ////cfg.LoadConfigsFromFileIfExists(@"C:\Davide\Progetti\ConsoleApplication1\EdmxCustomizer.config");




            //var guid1 = ParseGuid("230049BBFDBD0082E0530AA01448D117");
            //var guid2 = ParseGuid("230049BBFDBF0082E0530AA01448D117");
            //var res = guid1 == guid2;

            //var mgr = new EdmxManager(@"R:\Davide\GrinDbContext.edmx");
            //var mgr = new EdmxManager(@"C:\Davide\Progetti\ConsoleApplication1\ConsoleApplication1\GrinModel.edmx", null, null);
            //var mgr = new EdmxManager(@"C:\tfs\GRIN\dev\src\Gse.Grin.Platform.Solution\Gse.Grin.DataBaseContext.EF\GrinDbContext.edmx", null, null);

            //cfg.ManualOperations.Add(new ManualOperation() { Type = ManualOperationType.RemoveField, TableName = "TL_LRD_LOG_RICH_DETT", FieldName = "CON_ID_CONVENZIONE" });
            //cfg.Write(cfgFileName);

            {
                //mgr.FieldsManualOperations();
                //mgr.FixTabelleECampiEliminati();
                //if (!mgr.AssociationContainsDifferentTypes())
                //    mgr.FixTabelleNonPresentiInConceptual();
                //mgr.FixAssociations();
                //mgr.FixPropertiesAttributes();
                //mgr.FixConceptualModelNames();
                //     return;
            }


            //mgr.FieldsManualOperations();
            //mgr.FixTabelleECampiEliminati();

            ////mgr = new EdmxManager(@"C:\Davide\test.edmx", null, null);
            ////mgr.Avvia();
            //mgr.AssociationContainsDifferentTypes();
            //mgr.FixTabelleNonPresentiInConceptual();
            //mgr.FixAssociations();
            //mgr.FixPropertiesAttributes();
            ////mgr.ClearEdmxPreservingKeyFields();
            //mgr.FixConceptualModelNames();

            //mgr.FixTabelleECampiEliminati();
            //var edited = mgr.Salva();
            //if (mgr.StorageTypeNotManaged.Count > 0)
            //    System.Console.WriteLine("Tipi non gestiti:" + string.Join(",", mgr.StorageTypeNotManaged));
            System.Console.WriteLine("Premere un tasto per uscire.");
            System.Console.ReadKey();
        }


        private static void SwapArrayElements<T>(ref T[] array, int i, int j)
        {
            if (i == j || i >= array.Length || j >= array.Length) return;
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }

        private static Guid ParseGuid(string stringGuid)
        {
            byte[] bGuid = Guid.Parse(stringGuid).ToByteArray();
            SwapArrayElements(ref bGuid, 0, 3);
            SwapArrayElements(ref bGuid, 1, 2);
            SwapArrayElements(ref bGuid, 4, 5);
            SwapArrayElements(ref bGuid, 6, 7);
            return new Guid(bGuid);
        }
        public static string OracleToDotNet(string text)
        {
            byte[] bytes = ParseHex(text);
            Guid guid = new Guid(bytes);
            return guid.ToString("N").ToUpperInvariant();
        }

        public static string DotNetToOracle(string text)
        {
            Guid guid = new Guid(text);
            return BitConverter.ToString(guid.ToByteArray()).Replace("-", "");
        }

        static byte[] ParseHex(string text)
        {
            byte[] ret = new byte[text.Length / 2];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
            }
            return ret;
        }


        static void TestTfs()
        {
            var fullPath = @"C:\tfs\GRIN\dev-rin1\src\Gse.Grin.Platform.Solution\Gse.Grin.DataBaseContext.EF\TA_ACC_ACTION_CONTROL.cs";
            var proj = new FileInfo(@"C:\tfs\GRIN\dev-rin1\src\Gse.Grin.Platform.Solution\Gse.Grin.DataBaseContext.EF\Gse.Grin.DataBaseContext.EF.csproj");
            var tfsHelper = new TfsHelper(proj.FullName);
            tfsHelper.Connect();
            tfsHelper.PendAdd(fullPath);
            tfsHelper.Close();
        }

    }
}
