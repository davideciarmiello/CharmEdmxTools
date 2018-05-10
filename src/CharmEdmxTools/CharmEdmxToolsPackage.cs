using System;
using System.ComponentModel.Design;
using System.Runtime.Caching;
using System.Runtime.InteropServices;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

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

        private EdmxFixInvoker invoker;
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            invoker = new EdmxFixInvoker(this);
            _dte2 = invoker._dte2;
            if (invoker._dte2 == null)
            {
                return;
            }

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandID1 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxExecAllFixs);
                var menuItem1 = new OleMenuCommand(EdmxContextMenuItemCallback, null, invoker.OnOptimizeContextBeforeQueryStatus, menuCommandID1);
                mcs.AddCommand(menuItem1);

                var menuCommandID2 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxClearAllProperties);
                var menuItem2 = new OleMenuCommand(EdmxContextMenuItemCallback, null, invoker.OnOptimizeContextBeforeQueryStatus, menuCommandID2);
                mcs.AddCommand(menuItem2);

                var menuCommandID3 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)0x0025);
                var menuItem3 = new OleMenuCommand(EdmxMenuToolbarItemCallback, null, invoker.OnOptimizeMenuToolbarBeforeQueryStatus, menuCommandID3);
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
            invoker.ExecEdmxFix(selectedItem, id);
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
            invoker.ExecEdmxFix(selectedItem, id);
        }


        ///// <summary>
        ///// This function is the callback used to execute a command when the a menu item is clicked.
        ///// See the Initialize method to see how the menu item is associated to this function using
        ///// the OleMenuCommandService service and the MenuCommand class.
        ///// </summary>
        //private void MenuItemCallback(object sender, EventArgs e)
        //{
        //    //// Show a Message Box to prove we were here
        //    //IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
        //    //Guid clsid = Guid.Empty;
        //    //int result;
        //    //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
        //    //           0,
        //    //           ref clsid,
        //    //           "CharmEdmxTools",
        //    //           string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
        //    //           string.Empty,
        //    //           0,
        //    //           OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //    //           OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
        //    //           OLEMSGICON.OLEMSGICON_INFO,
        //    //           0,        // false
        //    //           out result));

        //    // Show a Message Box to prove we were here
        //    IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
        //    //string st1;
        //    //uiShell.GetAppName(out st1);
        //    string msg = String.Empty;
        //    var icon = OLEMSGICON.OLEMSGICON_INFO;
        //    try
        //    {
        //        //MessageBox.Show(
        //        //    "Generating views for Entity Framework version 6 is currently not supported.",
        //        //    "Entity Framework Power Tools",
        //        //    MessageBoxButtons.OK,
        //        //    MessageBoxIcon.Error);

        //        //Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame ppWindowFrame;
        //        //string pbstrData;
        //        //object ppunk;
        //        //uiShell.GetCurrentBFNavigationItem(out ppWindowFrame, out pbstrData, out ppunk);
        //        //var documentMoniker = ((dynamic)ppWindowFrame).DocumentMoniker;
        //        var currDoc = _dte2.ItemOperations.DTE.ActiveDocument;
        //        var documentMoniker = currDoc == null ? null : currDoc.FullName;
        //        msg = string.Format(CultureInfo.CurrentCulture, "{0}", documentMoniker);



        //        var proj = _dte2.SelectedItems.Item(1).ProjectItem;
        //        _dte2.ItemOperations.DTE.ActiveWindow.Close(vsSaveChanges.vsSaveChangesYes);
        //        if (!proj.Saved)
        //        {
        //            proj.Save();
        //        }
        //        msg += proj.Saved;
        //        var value = proj.Properties.Item("FullPath").Value as string;

        //        var isopende = _dte2.ItemOperations.IsFileOpen(value);

        //        bool test = false;
        //        if (test)
        //        {
        //            _dte2.ItemOperations.OpenFile(value);

        //        }

        //        msg += value;
        //        //var mgr = new EdmxManager(msg);
        //        //mgr.Avvia();
        //        //if (mgr.Salva())
        //        //    msg = "File salvato correttamente. Rieseguire lo strumento personalizzato.";
        //        //else
        //        //    msg = "Non è stata apportata nessuna modifica.";
        //        //if (mgr.StorageTypeNotManaged.Any())
        //        //    msg += " - Tipi non gestiti: " + string.Join(",", mgr.StorageTypeNotManaged);
        //    }
        //    catch (Exception ex)
        //    {
        //        //msg = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString());
        //        msg = ex.ToString();
        //        icon = OLEMSGICON.OLEMSGICON_CRITICAL;
        //    }
        //    //msg = EdmxCustomizerCommon.
        //    Guid clsid = Guid.Empty;
        //    int result;
        //    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
        //               0,
        //               ref clsid,
        //               "EdmxCustomizer",
        //               msg,
        //               string.Empty,
        //               0,
        //               OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
        //               icon,
        //               0,        // false
        //               out result));
        //}




        internal static class FileExtensions
        {
            public const string CSharp = ".cs";
            public const string VisualBasic = ".vb";
            public const string EntityDataModel = ".edmx";
            public const string Xml = ".xml";
            public const string Sql = ".sql";
        }
    }
}
