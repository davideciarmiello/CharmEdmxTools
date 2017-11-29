using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using CharmEdmxTools.EdmxConfig;
using CharmEdmxTools.EdmxUtils;
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
        }

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
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Visible = true;
            return;

            if (_dte2.ActiveDocument == null)
                menuCommand.Visible = false;
            else
                menuCommand.Visible = _dte2.ActiveDocument.Name.ToLower().EndsWith(FileExtensions.EntityDataModel);
        }

        public void ExecEdmxFix(ProjectItem selectedItem, int commandId)
        {
            var edmxPath = selectedItem.Properties.Item("FullPath").Value as string;

            if (!edmxPath.EndsWith(FileExtensions.EntityDataModel, System.StringComparison.OrdinalIgnoreCase))
                return;

            string configCreatedPath;
            var config = GetConfigForItem(selectedItem, out configCreatedPath);

            var logger = GetOutputPaneWriteFunction();

            if (configCreatedPath != null)
            {
                logger(string.Format(Messages.Current.CreatedConfig, configCreatedPath));
                return;
            }

            logger(string.Format(Messages.Current.Avvioelaborazionedi, selectedItem.Name));

            var edmxDocument = selectedItem.Document;
            if (edmxDocument != null && !edmxDocument.Saved)
                edmxDocument.Save();

            var mgr = new EdmxManager(edmxPath, logger, config);

            if (commandId == PkgCmdIDList.cmdidEdmxExecAllFixs)
            {
                mgr.FieldsManualOperations();
                mgr.FixTabelleECampiEliminati();
                if (!mgr.AssociationContainsDifferentTypes())
                    mgr.FixTabelleNonPresentiInConceptual();
                mgr.FixAssociations();
                mgr.FixPropertiesAttributes();
                mgr.FixConceptualModelNames();
            }
            else if (commandId == PkgCmdIDList.cmdidEdmxClearAllProperties)
            {
                mgr.ClearEdmxPreservingKeyFields();
            }

            if (mgr.IsChanged())
            {
                var designerIsOpened = false;
                if (edmxDocument != null && edmxDocument.ActiveWindow != null)
                {
                    edmxDocument.Close(vsSaveChanges.vsSaveChangesNo);
                    designerIsOpened = true;
                }
                logger(Messages.Current.SavingEdmx);
                mgr.Salva();
                logger(Messages.Current.RielaborazioneEdmx);
                var window = _dte2.ItemOperations.OpenFile(edmxPath);
                window.Document.Save(); // faccio rielaborare i T4
                if (!designerIsOpened)
                    window.Document.Close(vsSaveChanges.vsSaveChangesYes);
                logger(Messages.Current.OperazioneTerminataConSuccesso);
            }
            else
            {
                logger(Messages.Current.OperazioneTerminataSenzaModifiche);
            }

            if (config.SccPocoFixer.Enabled)
            {
                var ttname = selectedItem.Name.Remove(selectedItem.Name.Length - 4) + "tt";
                var ttItem = selectedItem.ProjectItems.OfType<ProjectItem>().FirstOrDefault(it => string.Equals(it.Name, ttname, StringComparison.OrdinalIgnoreCase));
                if (ttItem != null && _dte2.SourceControl != null)
                {
                    var csname = selectedItem.Name.Remove(selectedItem.Name.Length - 4) + "cs";
                    var allItems = ttItem.ProjectItems.OfType<ProjectItem>().Where(it => !string.Equals(it.Name, csname, System.StringComparison.OrdinalIgnoreCase)).ToList();
                    var sscMgr = GetSccManager();
                    foreach (var item in allItems)
                    {
                        if (EnsureAddFileToSccIfExists(item, ttItem, sscMgr))
                        {
                            logger(string.Format(Messages.Current.AggiuntoFileASourceControl, item.Name));
                        }
                    }
                }
            }
        }

        private CharmEdmxConfiguration GetConfigForItem(ProjectItem selectedItem, out string configCreatedPath)
        {
            configCreatedPath = null;
            var config = ConfigCache.Get(selectedItem.ContainingProject.FullName) as CharmEdmxConfiguration;
            if (config != null)
                return config;
            var cfgProj = string.Concat(selectedItem.ContainingProject.FullName, ".CharmEdmxTools");
            var cfgSln = string.Concat(selectedItem.DTE.Solution.FullName, ".CharmEdmxTools");
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
            ConfigCache.Add(selectedItem.ContainingProject.FullName, config, policy);
            return config;
        }

        private IVsSccManager2 GetSccManager()
        {
            var sscMgr = ServiceProvider.GetService(typeof(SVsSccManager)) as IVsSccManager2;
            if (sscMgr == null)
                return null;
            int installed = 0;
            if (!ErrorHandler.Succeeded(sscMgr.IsInstalled(out installed)) || (installed == 0))
                return null;
            return sscMgr;
        }

        private bool EnsureAddFileToSccIfExists(ProjectItem item, ProjectItem itemParent, IVsSccManager2 sscMgr)
        {
            var fullPath = item.Properties.Item("FullPath").Value as string;
            if (!System.IO.File.Exists(fullPath))
                return false;
            if (item.DTE.SourceControl != null && !item.DTE.SourceControl.IsItemUnderSCC(fullPath))
            {
                if (sscMgr != null)
                {
                    var icons = new VsStateIcon[1];
                    var status = new uint[1];
                    var res = sscMgr.GetSccGlyph(1, new[] { fullPath }, icons, status);
                    if (res == VSConstants.S_FALSE && icons != null && icons.Length > 0 && icons[0] != VsStateIcon.STATEICON_BLANK)
                    {
                        // già è stato aggiunto
                        return false;
                    }
                }

                var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(item.ContainingProject.FullName);
                var serverUri = workspaceInfo.ServerUri;

                using (var connection = new TfsTeamProjectCollection(serverUri))
                {
                    var vcs = connection.GetService<VersionControlServer>();
                    var workspace = vcs.GetWorkspace(item.ContainingProject.FullName);

                    var pendingChanges = workspace.GetPendingChanges();
                    var dd = pendingChanges.Where(it => it.LocalItem == fullPath && it.IsDelete).ToArray();
                    if (dd.Length > 0)
                    {
                        var items = dd.Select(it => it.LocalItem).ToList();
                        items.ForEach(it =>
                        {
                            try { System.IO.File.Copy(it, it + ".tmptfs", true); File.Delete(it); }
                            catch { }
                        });
                        workspace.Undo(dd);
                        items.ForEach(it =>
                        {
                            try { System.IO.File.Copy(it + ".tmptfs", it, true); File.Delete(it + ".tmptfs"); }
                            catch { }
                        });
                    }
                    workspace.PendAdd(fullPath);

                    try
                    {
                        Workstation.Current.EnsureUpdateWorkspaceInfoCache(vcs, vcs.AuthorizedUser);
                    }
                    catch { }
                }

                return true;
            }
            return false;
        }

        private Action<string> GetOutputPaneWriteFunction(string name = "Charm Edmx Tools", Guid? guid = null)
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

    }
}
