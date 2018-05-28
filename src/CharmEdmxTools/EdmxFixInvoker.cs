using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using AppCodeShared;
using CharmEdmxTools.Core.CoreGlobalization;
using CharmEdmxTools.Core.EdmxConfig;
using CharmEdmxTools.Core.ExtensionsMethods;
using CharmEdmxTools.Core.Manager;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CharmEdmxTools
{
    public class EdmxFixInvoker
    {
        public EdmxFixInvoker(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            _dte2 = ServiceProvider.GetService(typeof(DTE)) as DTE2;
            if (_dte2 != null)
            {
                DteVersion = Convert.ToInt32(_dte2.Version.Split('.').First());
            }
        }

        public int DteVersion { get; set; }

        private IServiceProvider ServiceProvider;

        private ObjectCache ConfigCache = MemoryCache.Default;

        public DTE2 _dte2;


        public void OnOptimizeContextBeforeQueryStatus(object sender, EventArgs e)
        {
            OnItemMenuBeforeQueryStatus(sender, new[] { FileExtensions.EntityDataModel });
        }
        private void OnItemMenuBeforeQueryStatus(object sender, IEnumerable<string> supportedExtensions)
        {
            Contract.Requires(supportedExtensions != null);

            var menuCommand = sender as MenuCommand;

            if (menuCommand == null)
            {
                return;
            }
            //menuCommand.Visible = true;
            //return;
            if (_dte2.SelectedItems.Count != 1)
            {
                menuCommand.Visible = false;
                return;
            }

            var extensionValue = GetSelectedItemExtension();
            menuCommand.Visible = supportedExtensions.Contains(extensionValue);
        }

        public void OnOptimizeMenuToolbarBeforeQueryStatus(object sender, EventArgs e)
        {
            //System.Diagnostics.Debugger.Break();
            //System.Diagnostics.Debugger.Launch();
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;

            //menuCommand.Visible = true;
            //return;

            if (_dte2.ActiveDocument == null)
                menuCommand.Visible = false;
            else
                menuCommand.Visible = _dte2.ActiveDocument.Name.ToLowerInvariant().EndsWith(FileExtensions.EntityDataModel);
        }

        public bool ExecAllFixsWithoutSave(Document selectedDocument)
        {
            return ExecEdmxFix(null, selectedDocument, (int)PkgCmdIDList.cmdidEdmxExecAllFixs, true);
        }

        public bool Fixing { get; private set; }
        public bool ExecEdmxFix(ProjectItem selectedItem, Document selectedDocument, int commandId, bool skipSave = false)
        {
            if (selectedItem != null && selectedItem.Properties == null && selectedDocument != null)
                selectedItem = null;
            var edmxPath = selectedItem == null ? selectedDocument.FullName : selectedItem.Properties.Item("FullPath").Value as string;

            if (!edmxPath.EndsWith(FileExtensions.EntityDataModel, System.StringComparison.OrdinalIgnoreCase))
                return false;

            string configCreatedPath;
            var config = GetConfigForItem(selectedItem, selectedDocument, true, out configCreatedPath);

            var logger = GetOutputPaneWriteFunction();
            Fixing = true;
            try
            {
                if (configCreatedPath != null)
                {
                    logger(string.Format(Messages.Current.CreatedConfig, configCreatedPath));
                    return false;
                }

                logger(string.Format(Messages.Current.Avvioelaborazionedi,
                    selectedDocument != null ? selectedDocument.Name : selectedItem.Name));

                var sw = Stopwatch.StartNew();
                var edmxDocument = selectedDocument ?? selectedItem.Document;
                var edmxXml = edmxDocument != null ? GetDocumentText(edmxDocument) : null;

                //if (edmxDocument != null && !edmxDocument.Saved)
                //{
                //    edmxDocument.Save();
                //    logger(string.Format(Messages.Current.SavedEdmxIn, sw.Elapsed));
                //}

                sw.Restart();
                var mgr = new EdmxManager(edmxPath, edmxXml, logger, config);

                if (commandId == PkgCmdIDList.cmdidEdmxExecAllFixs)
                {
                    mgr.ExecAllFixs();
                }
                else if (commandId == PkgCmdIDList.cmdidEdmxClearAllProperties)
                {
                    mgr.ClearEdmxPreservingKeyFields();
                }

                var tempoEsecuzioneFixs = sw.Elapsed;
                var changed = mgr.IsChanged();
                if (changed)
                {
                    //var designerIsOpened = false;
                    //if (edmxDocument != null && edmxDocument.ActiveWindow != null)
                    //{
                    //    //edmxDocument.Close(vsSaveChanges.vsSaveChangesNo);
                    //    designerIsOpened = true;
                    //}

                    var windowOpened = edmxDocument == null ? _dte2.ItemOperations.OpenFile(edmxPath) : null;
                    if (edmxDocument == null)
                        edmxDocument = windowOpened.Document;

                    SetDocumentText(edmxDocument, mgr._xDoc.ToString());

                    if (tempoEsecuzioneFixs > TimeSpan.FromSeconds(1))
                        logger(string.Format(Messages.Current.SavingEdmxAfterFixIn, tempoEsecuzioneFixs));
                    //mgr.Salva();
                    sw.Restart();
                    logger(Messages.Current.RielaborazioneEdmx);
                    //var window = _dte2.ItemOperations.OpenFile(edmxPath);
                    //window.Document.Save(); // faccio rielaborare i T4
                    //if (!designerIsOpened)
                    //    window.Document.Close(vsSaveChanges.vsSaveChangesYes);
                    if (!skipSave)
                        edmxDocument.Save();

                    if (windowOpened != null)
                        windowOpened.Document.Close(vsSaveChanges.vsSaveChangesNo);
                    sw.Stop();
                    logger(string.Format(Messages.Current.OperazioneTerminataConSuccessoIn, sw.Elapsed));
                    if (windowOpened == null && !skipSave)
                        mgr.Salva();
                }
                else
                {
                    if (edmxDocument != null && !edmxDocument.Saved && !skipSave)
                    {
                        //sw.Restart();
                        logger(Messages.Current.RielaborazioneEdmx);
                        edmxDocument.Save();
                        tempoEsecuzioneFixs = sw.Elapsed;
                        //logger(string.Format(Messages.Current.SavedEdmxIn, sw.Elapsed));
                    }

                    logger(string.Format(Messages.Current.OperazioneTerminataSenzaModificheIn, tempoEsecuzioneFixs));
                }

                //if (config.SccPocoFixer.Enabled && selectedItem != null)
                //{
                //    sw.Restart();
                //    logger(string.Format(Messages.Current.AvvioVerificaFilesSourceControl));
                //    var ttname = selectedItem.Name.Remove(selectedItem.Name.Length - 4) + "tt";
                //    var ttItem = selectedItem.ProjectItems.OfType<ProjectItem>().FirstOrDefault(it => string.Equals(it.Name, ttname, StringComparison.OrdinalIgnoreCase));
                //    if (ttItem != null && _dte2.SourceControl != null)
                //    {
                //        var csname = selectedItem.Name.Remove(selectedItem.Name.Length - 4) + "cs";
                //        var allItems = ttItem.ProjectItems.OfType<ProjectItem>().Where(it => !string.Equals(it.Name, csname, System.StringComparison.OrdinalIgnoreCase)).ToList();
                //        var sscMgr = GetSccManager();
                //        TfsHelper tfsHelper = null;
                //        foreach (var item in allItems)
                //        {
                //            if (EnsureAddFileToSccIfExists(item, ttItem, sscMgr, ref tfsHelper))
                //            {
                //                logger(string.Format(Messages.Current.AggiuntoFileASourceControl, item.Name));
                //            }
                //        }
                //        if (tfsHelper != null)
                //        {
                //            tfsHelper.Close();
                //        }
                //    }
                //    logger(string.Format(Messages.Current.OperazioneTerminataConSuccessoIn, sw.Elapsed));
                //}
                return changed;
            }
            catch (Exception ex)
            {
                logger("ERROR: " + ex);
                return false;
            }
            finally
            {
                Fixing = false;
            }
        }

        public CharmEdmxConfiguration GetConfigForItem(ProjectItem selectedItem, Document selectedDocument, bool autoCreate, out string configCreatedPath)
        {
            var cfgProj = selectedItem != null ? selectedItem.ContainingProject.FullName : null;
            var cfgSln = selectedItem != null ? selectedItem.DTE.Solution.FullName : null;
            if (selectedItem == null)
            {
                var fi = new FileInfo(selectedDocument.FullName).Directory;
                while (fi != null && (cfgProj == null || cfgSln == null))
                {
                    cfgProj = cfgProj ?? fi.EnumerateFiles("*.csproj").OrderByDescending(x => x.LastWriteTimeUtc)
                                  .Select(x => x.FullName).FirstOrDefault();
                    cfgSln = cfgSln ?? fi.EnumerateFiles("*.sln").OrderByDescending(x => x.LastWriteTimeUtc)
                                  .Select(x => x.FullName).FirstOrDefault();
                    fi = fi.Parent;
                }

                if (cfgProj == null && cfgSln == null)
                    cfgProj = selectedDocument.FullName;
                cfgProj = cfgProj ?? cfgSln;
                cfgSln = cfgSln ?? cfgProj;
            }
            return GetConfigForItem(cfgProj, cfgSln, autoCreate, out configCreatedPath);
        }

        public CharmEdmxConfiguration GetConfigForItem(string proj, string sln, bool autoCreate, out string configCreatedPath)
        {
            configCreatedPath = null;
            var config = ConfigCache.Get(proj) as CharmEdmxConfiguration;
            if (config != null)
                return config;
            var cfgProj = string.Concat(proj, ".CharmEdmxTools");
            var cfgSln = string.Concat(sln, ".CharmEdmxTools");
            var lstFile = new List<string>();
            if (System.IO.File.Exists(cfgProj))
            {
                config = CharmEdmxConfiguration.Load(cfgProj);
                lstFile.Add(cfgProj);
            }
            else if (System.IO.File.Exists(cfgSln))
            {
                config = CharmEdmxConfiguration.Load(cfgSln);
                lstFile.Add(cfgSln);
            }
            else
            {
                if (autoCreate == false)
                    return null;
                config = new CharmEdmxConfiguration();
                config.FillDefaultConfiguration();
                config.SccPocoFixer.Enabled = GetSccManager() != null;
                config.SccPocoFixer.SccPlugin = "tfs";
                config.Write(cfgSln);
                configCreatedPath = cfgSln;
                lstFile.Add(cfgSln);
            }
            var policy = new CacheItemPolicy();
            policy.ChangeMonitors.Add(new HostFileChangeMonitor(lstFile));
            ConfigCache.Add(proj, config, policy);
            return config;
        }

        public IVsSccManager2 GetSccManager()
        {
            var sscMgr = ServiceProvider.GetService(typeof(SVsSccManager)) as IVsSccManager2;
            if (sscMgr == null)
                return null;
            int installed = 0;
            if (!ErrorHandler.Succeeded(sscMgr.IsInstalled(out installed)) || (installed == 0))
                return null;
            return sscMgr;
        }

        //private bool EnsureAddFileToSccIfExists(ProjectItem item, ProjectItem itemParent, IVsSccManager2 sscMgr, ref TfsHelper tfsHelper)
        //{
        //    var fullPath = item.Properties.Item("FullPath").Value as string;
        //    if (!System.IO.File.Exists(fullPath))
        //        return false;
        //    if (item.DTE.SourceControl != null && !item.DTE.SourceControl.IsItemUnderSCC(fullPath))
        //    {
        //        if (sscMgr != null)
        //        {
        //            var icons = new VsStateIcon[1];
        //            var status = new uint[1];
        //            var res = sscMgr.GetSccGlyph(1, new[] { fullPath }, icons, status);
        //            if (res == VSConstants.S_FALSE && icons != null && icons.Length > 0 && icons[0] != VsStateIcon.STATEICON_BLANK)
        //            {
        //                // già è stato aggiunto
        //                return false;
        //            }
        //        }

        //        if (tfsHelper == null)
        //        {
        //            tfsHelper = new TfsHelper(item.ContainingProject.FullName);
        //            tfsHelper.Connect(true);
        //        }

        //        tfsHelper.PendAdd(fullPath);

        //        return true;
        //    }
        //    return false;
        //}

        public Action<string> GetOutputPaneWriteFunction(string name = "Charm Edmx Tools", Guid? guid = null)
        {
            var customPane = _dte2.ToolWindows.OutputWindow.OutputWindowPanes.OfType<OutputWindowPane>().FirstOrDefault(it => it.Name == name);
            if (customPane == null)
            {
                IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                Guid customGuid = guid.HasValue ? guid.Value : Guid.NewGuid();
                string customTitle = name;
                outWindow.CreatePane(ref customGuid, customTitle, 1, 1);
                customPane = _dte2.ToolWindows.OutputWindow.OutputWindowPanes.OfType<OutputWindowPane>().FirstOrDefault(it => it.Name == name);
            }
            //var allPanes = _dte2.ToolWindows.OutputWindow.OutputWindowPanes.OfType<OutputWindowPane>();
            //var outputPane = allPanes.Where(it => it.Name == name).FirstOrDefault();
            //if (outputPane == null)
            //    outputPane = _dte2.ToolWindows.OutputWindow.OutputWindowPanes.Add(name);
            var outputPane = customPane;
            if (outputPane == null)
                return new System.Action<string>(s => { });
            //var toActivate = true;
            return new System.Action<string>(s =>
            {
                Trace.WriteLine(s);
                //if (toActivate)
                //{
                //     toActivate = false;
                //}
                outputPane.OutputString(s + Environment.NewLine);
                outputPane.Activate();
            });
        }

        List<ProjectItems> PrintProjectItemslist = new List<ProjectItems>();
        public void PrintProjectItems(ProjectItems items)
        {
            if (PrintProjectItemslist.Contains(items))
                return;
            PrintProjectItemslist.Add(items);
            foreach (ProjectItem item in items)
            {
                Console.WriteLine(item.Name + " - " + item.FileCount);
            }

            foreach (ProjectItem item in items)
            {
                if (item.ProjectItems.Count > 0)
                    PrintProjectItems(item.ProjectItems);
            }


        }


        private string GetSelectedItemExtension()
        {
            var selectedItem = _dte2.SelectedItems.Item(1);

            if ((selectedItem.ProjectItem == null)
                || (selectedItem.ProjectItem.Properties == null))
            {
                return null;
            }

            var extension = selectedItem.ProjectItem.Properties.Item("Extension");

            if (extension == null)
            {
                return null;
            }

            return (string)extension.Value;
        }

        public static string GetDocumentText(Document document)
        {
            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            var content = editPoint.GetText(textDocument.EndPoint);
            return content;
        }

        private static void SetDocumentText(Document document, string content)
        {
            var textDocument = (TextDocument)document.Object("TextDocument");
            EditPoint editPoint = textDocument.StartPoint.CreateEditPoint();
            EditPoint endPoint = textDocument.EndPoint.CreateEditPoint();
            editPoint.ReplaceText(endPoint, content, 0);
            //document.Save();
        }

    }
}
