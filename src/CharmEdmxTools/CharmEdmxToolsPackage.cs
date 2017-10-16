using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.InteropServices;
using System.Web;
using CharmEdmxTools.EdmxConfig;
using CharmEdmxTools.EdmxUtils;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

//using Microsoft.DbContextPackage.Extensions;
//using Microsoft.DbContextPackage.Handlers;
//using Microsoft.DbContextPackage.Resources;
//using Microsoft.DbContextPackage.Utilities;
//using Microsoft.VisualStudio.Shell.Design;

namespace CharmEdmxTools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.1", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidCharmEdmxToolsPkgString)]
    //[ProvideAutoLoad(UIContextGuids.SolutionHasSingleProject)] 
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class CharmEdmxToolsPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public CharmEdmxToolsPackage()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        DTE2 _dte2;
        internal DTE2 DTE2
        {
            get { return _dte2; }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            _dte2 = GetService(typeof(DTE)) as DTE2;

            if (_dte2 == null)
            {
                return;
            }

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandID1 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxExecAllFixs);
                var menuItem1 = new OleMenuCommand(EdmxContextMenuItemCallback, null, OnOptimizeContextBeforeQueryStatus, menuCommandID1);
                mcs.AddCommand(menuItem1);

                var menuCommandID2 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxClearAllProperties);
                var menuItem2 = new OleMenuCommand(EdmxContextMenuItemCallback, null, OnOptimizeContextBeforeQueryStatus, menuCommandID2);
                mcs.AddCommand(menuItem2);

                var menuCommandID3 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)0x0025);
                var menuItem3 = new OleMenuCommand(EdmxMenuToolbarItemCallback, null, OnOptimizeMenuToolbarBeforeQueryStatus, menuCommandID3);
                mcs.AddCommand(menuItem3);
            }
        }
        #endregion


        //private static System.Collections.Concurrent.ConcurrentDictionary<string, CharmEdmxConfiguration> ConfigCache = new System.Collections.Concurrent.ConcurrentDictionary<string, CharmEdmxConfiguration>();
        private ObjectCache ConfigCache = MemoryCache.Default;

        private void EdmxMenuToolbarItemCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;
            if (_dte2.ActiveDocument == null)
                return;
            var selectedItem = _dte2.ActiveDocument.ProjectItem;
            var id = menuCommand.CommandID.ID;
            if (id == 0x0025)
                id = (int)PkgCmdIDList.cmdidEdmxExecAllFixs;
            ExecEdmxFix(selectedItem, id);
        }

        private void EdmxContextMenuItemCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;

            if (_dte2.SelectedItems.Count != 1)
                return;

            var selectedItem = _dte2.SelectedItems.Item(1).ProjectItem;
            var id = menuCommand.CommandID.ID;
            ExecEdmxFix(selectedItem, id);
        }

        private void ExecEdmxFix(ProjectItem selectedItem, int commandId)
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
            var sscMgr = GetService(typeof(SVsSccManager)) as IVsSccManager2;
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


        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            //// Show a Message Box to prove we were here
            //IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            //Guid clsid = Guid.Empty;
            //int result;
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
            //           0,
            //           ref clsid,
            //           "CharmEdmxTools",
            //           string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
            //           string.Empty,
            //           0,
            //           OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //           OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
            //           OLEMSGICON.OLEMSGICON_INFO,
            //           0,        // false
            //           out result));

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            //string st1;
            //uiShell.GetAppName(out st1);
            string msg = String.Empty;
            var icon = OLEMSGICON.OLEMSGICON_INFO;
            try
            {
                //MessageBox.Show(
                //    "Generating views for Entity Framework version 6 is currently not supported.",
                //    "Entity Framework Power Tools",
                //    MessageBoxButtons.OK,
                //    MessageBoxIcon.Error);

                //Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame ppWindowFrame;
                //string pbstrData;
                //object ppunk;
                //uiShell.GetCurrentBFNavigationItem(out ppWindowFrame, out pbstrData, out ppunk);
                //var documentMoniker = ((dynamic)ppWindowFrame).DocumentMoniker;
                var currDoc = _dte2.ItemOperations.DTE.ActiveDocument;
                var documentMoniker = currDoc == null ? null : currDoc.FullName;
                msg = string.Format(CultureInfo.CurrentCulture, "{0}", documentMoniker);



                var proj = _dte2.SelectedItems.Item(1).ProjectItem;
                _dte2.ItemOperations.DTE.ActiveWindow.Close(vsSaveChanges.vsSaveChangesYes);
                if (!proj.Saved)
                {
                    proj.Save();
                }
                msg += proj.Saved;
                var value = proj.Properties.Item("FullPath").Value as string;

                var isopende = _dte2.ItemOperations.IsFileOpen(value);

                bool test = false;
                if (test)
                {
                    _dte2.ItemOperations.OpenFile(value);

                }

                msg += value;
                //var mgr = new EdmxManager(msg);
                //mgr.Avvia();
                //if (mgr.Salva())
                //    msg = "File salvato correttamente. Rieseguire lo strumento personalizzato.";
                //else
                //    msg = "Non è stata apportata nessuna modifica.";
                //if (mgr.StorageTypeNotManaged.Any())
                //    msg += " - Tipi non gestiti: " + string.Join(",", mgr.StorageTypeNotManaged);
            }
            catch (Exception ex)
            {
                //msg = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString());
                msg = ex.ToString();
                icon = OLEMSGICON.OLEMSGICON_CRITICAL;
            }
            //msg = EdmxCustomizerCommon.
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "EdmxCustomizer",
                       msg,
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       icon,
                       0,        // false
                       out result));
        }




        internal static class FileExtensions
        {
            public const string CSharp = ".cs";
            public const string VisualBasic = ".vb";
            public const string EntityDataModel = ".edmx";
            public const string Xml = ".xml";
            public const string Sql = ".sql";
        }
        private void OnOptimizeContextBeforeQueryStatus(object sender, EventArgs e)
        {
            OnItemMenuBeforeQueryStatus(
                sender,
                new[] { FileExtensions.EntityDataModel });
        }
        private void OnOptimizeMenuToolbarBeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;
            if (_dte2.ActiveDocument == null)
                menuCommand.Visible = false;
            else
                menuCommand.Visible = _dte2.ActiveDocument.Name.ToLower().EndsWith(FileExtensions.EntityDataModel);
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
