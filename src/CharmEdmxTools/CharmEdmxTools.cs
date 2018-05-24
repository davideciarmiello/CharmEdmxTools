using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
//using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CharmEdmxTools
{
    /// <summary>
    /// Command handler
    /// </summary>
    public sealed class CharmEdmxTools : IVsTrackProjectDocumentsEvents2, IDisposable
    {

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CharmEdmxTools Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            if (Instance != null)
                DisposePackage();
            Instance = new CharmEdmxTools(package);
        }

        public static void DisposePackage()
        {
            if (Instance == null)
                return;
            Instance.Dispose();
            Instance = null;
        }

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;


        private readonly EdmxFixInvoker _invoker;
        /// <summary>
        /// Initializes a new instance of the <see cref="CharmEdmxTools"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CharmEdmxTools(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this._package = package;

            _invoker = new EdmxFixInvoker(package);

            if (_invoker._dte2 == null)
            {
                return;
            }

            var mcs = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                var version = _invoker.DteVersion;
                var menuCommandId1 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxExecAllFixs);
                var menuItem1 = new OleMenuCommand(EdmxContextMenuItemCallback, null, _invoker.OnOptimizeContextBeforeQueryStatus, menuCommandId1) { Visible = false };
                mcs.AddCommand(menuItem1);

                var menuCommandId2 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxClearAllProperties);
                var menuItem2 = new OleMenuCommand(EdmxContextMenuItemCallback, null, _invoker.OnOptimizeContextBeforeQueryStatus, menuCommandId2) { Visible = false };
                mcs.AddCommand(menuItem2);

                var menuCommandId3 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, version < 14 ? (int)PkgCmdIDList.cmdidEdmxToolbarFixUpper : (int)PkgCmdIDList.cmdidEdmxToolbarFix);
                var menuItem3 = new OleMenuCommand(EdmxMenuToolbarItemCallback, null, _invoker.OnOptimizeMenuToolbarBeforeQueryStatus, menuCommandId3) { Visible = false };
                if (PkgCmdIDList.TopLevelMenu.HasValue)
                    menuItem3 = new OleMenuCommand(EdmxMenuToolbarItemCallback, null, null, menuCommandId3) { Visible = false };
                mcs.AddCommand(menuItem3);

                if (PkgCmdIDList.TopLevelMenu.HasValue)
                {
                    menuCommandId3 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.TopLevelMenu);
                    menuItem3 = new OleMenuCommand(null, null, _invoker.OnOptimizeMenuToolbarBeforeQueryStatus, menuCommandId3) { Visible = false };
                    mcs.AddCommand(menuItem3);
                }
            }

            var tracker = (IVsTrackProjectDocuments2)this.ServiceProvider.GetService(typeof(SVsTrackProjectDocuments));
            // ask VS to notify us of files & directories changes
            tracker.AdviseTrackProjectDocumentsEvents(this, out _trackerCookie);

            events = _invoker._dte2.Events;
            documentEvents = events.DocumentEvents;
            documentEvents.DocumentSaved += DocumentEventsOnDocumentSaved;
        }
        public void Dispose()
        {
            if (this._trackerCookie == 0)
                return;
            var tracker = (IVsTrackProjectDocuments2)this.ServiceProvider.GetService(typeof(SVsTrackProjectDocuments));
            tracker.UnadviseTrackProjectDocumentsEvents(_trackerCookie);
            _trackerCookie = 0;
            documentEvents.DocumentSaved -= DocumentEventsOnDocumentSaved;
        }

        private uint _trackerCookie;
        private EnvDTE.Events events;
        private DocumentEvents documentEvents;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        private void EdmxMenuToolbarItemCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;
            if (_invoker._dte2.ActiveDocument == null)
                return;
            var selectedItem = _invoker._dte2.ActiveDocument.ProjectItem;
            var id = menuCommand.CommandID.ID;
            if (id == PkgCmdIDList.cmdidEdmxToolbarFix || id == PkgCmdIDList.cmdidEdmxToolbarFixUpper)
                id = (int)PkgCmdIDList.cmdidEdmxExecAllFixs;
            _invoker.ExecEdmxFix(selectedItem, _invoker._dte2.ActiveDocument, id);
        }


        private void EdmxContextMenuItemCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;

            if (_invoker._dte2.SelectedItems.Count != 1)
                return;

            //var sscMgr = _invoker.GetSccManager();
            //var x = sscMgr.GetType();
            //System.Diagnostics.Debug.WriteLine(x.FullName);

            //SourceControl sc = null;

            //var file = @"C:\tfs\GRIN\dev\src\Gse.Grin.Platform.Solution\Gse.Grin.ReadModel.Notifiche\TA_GNE_GRIN_NEWS.cs";
            //ProjectItem fileitem;
            //foreach (SelectedItem dte2SelectedItem in _invoker._dte2.SelectedItems)
            //{
            //    var item = dte2SelectedItem.ProjectItem;
            //    var fullPath = item.Properties.Item("FullPath").Value as string;
            //    if (fullPath == file)
            //        fileitem = item;
            //    if (!System.IO.File.Exists(fullPath))
            //        continue;
            //    sc = item.DTE.SourceControl;
            //    var isItemUnderScc = item.DTE.SourceControl.IsItemUnderSCC(fullPath);
            //    var isItemCheckedOut = item.DTE.SourceControl.IsItemCheckedOut(fullPath);

            //    var icons = new VsStateIcon[1];
            //    var status = new uint[1];
            //    var res = sscMgr.GetSccGlyph(1, new[] { fullPath }, icons, status);
            //    if (res == VSConstants.S_FALSE && icons != null && icons.Length > 0 && icons[0] != VsStateIcon.STATEICON_BLANK)
            //    {
            //        // già è stato aggiunto
            //        //continue;
            //    }

            //    var msg = fullPath + " - isItemUnderScc: " + isItemUnderScc + " isItemCheckedOut: " + isItemCheckedOut
            //              + " GetSccGlyph: " + res + " icon: " + icons.FirstOrDefault();
            //    System.Diagnostics.Debug.WriteLine(msg);
            //}

            ////fileitem.Document.

            //var rexs = sc.CheckOutItem(file);

            //IVsHierarchy solHier = this.ServiceProvider.GetService(typeof(SVsSolution)) as IVsHierarchy;

            //IVsSccProject2 xx = solHier as IVsSccProject2;

            //IVsSolution sol = (IVsSolution)this.ServiceProvider.GetService(typeof(SVsSolution));
            //Guid rguidEnumOnlyThisType = new Guid();
            //IEnumHierarchies ppenum = null;
            //ErrorHandler.ThrowOnFailure(sol.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref rguidEnumOnlyThisType, out ppenum));
            //var lst = new System.Collections.Generic.List<IVsSccProject2>();
            //IVsHierarchy[] rgelt = new IVsHierarchy[1];
            //uint pceltFetched = 0;
            //while (ppenum.Next(1, rgelt, out pceltFetched) == VSConstants.S_OK &&
            //       pceltFetched == 1)
            //{
            //    IVsSccProject2 sccProject2 = rgelt[0] as IVsSccProject2;
            //    if (sccProject2 != null)
            //    {
            //        lst.Add(sccProject2);
            //        //mapHierarchies[rgelt[0]] = true;
            //    }
            //}

            //var xxx = lst[0] as IVsHierarchy;
            //var tracker = lst[0] as IVsTrackProjectDocumentsEvents2;



            ////tracker.OnAfterSccStatusChanged()

            ////solHier.AdviseHierarchyEvents( )
            ////_invoker._dte2.SelectedItems.Item(0).Project.

            ////var sccService = ServiceProvider.GetService<SourceControlProvider>();

            ////solHier.set

            //return;

            var selectedItem = _invoker._dte2.SelectedItems.Item(1).ProjectItem;
            var id = menuCommand.CommandID.ID;
            _invoker.ExecEdmxFix(selectedItem, null, id);
        }




        //public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        //{
        //    _applicationObject = (DTE2)application;
        //    _addInInstance = (AddIn)addInInst;

        //    // the Addin project needs assembly references to Microsoft.VisualStudio.Shell, Microsoft.VisualStudio.Shell.Interop && Microsoft.VisualStudio.OLE.Interop
        //    // any version should do

        //}

        public int OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags,
            VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        {
            var msg = "OnQueryAddFiles pProject:" + pProject + " file[0]:" + rgpszMkDocuments[0];
            Trace.WriteLine(msg);
            _invoker.GetOutputPaneWriteFunction()(msg);
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames,
            VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            //IVsSccProject2 sccProject = pProject as IVsSccProject2;


            //IVsHierarchy pHier = pProject as IVsHierarchy;



            //int iFound = 0;
            //uint itemId = 0;
            //VSDOCUMENTPRIORITY[] pdwPriority = new VSDOCUMENTPRIORITY[1];
            //int result = pProject.IsDocumentInProject(rgszMkOldNames[0], out iFound, pdwPriority, out itemId);
            //if (result != VSConstants.S_OK)
            //    throw new Exception("Unexpected error calling IVsProject.IsDocumentInProject");
            //if (iFound == 0)
            //    throw new Exception("Cannot retrieve ProjectItem for template file");
            //if (itemId == 0)
            //    throw new Exception("Cannot retrieve ProjectItem for template file");


            //Microsoft.VisualStudio.OLE.Interop.IServiceProvider itemContext = null;
            //result = pProject.GetItemContext(itemId, out itemContext);
            //if (result != VSConstants.S_OK)
            //    throw new Exception("Unexpected error calling IVsProject.GetItemContext");
            //if (itemContext == null)
            //    throw new Exception("IVsProject.GetItemContext returned null");

            //ServiceProvider itemContextService = new ServiceProvider(itemContext);
            //EnvDTE.ProjectItem templateItem = (EnvDTE.ProjectItem)itemContextService.GetService(typeof(EnvDTE.ProjectItem));

            //var fullPath = templateItem.Properties.Item("FullPath").Value as string;

            //var fileNameS = templateItem.Name;

            ////pHier.
            //string projectName = null;
            //if (sccProject == null)
            //{
            //    // This is the solution calling
            //    pHier = (IVsHierarchy)this.ServiceProvider.GetService(typeof(SVsSolution));
            //    //projectName = _sccProvider.GetSolutionFileName();
            //}
            //else
            //{
            //    // If the project doesn't support source control, it will be skipped
            //    //if (sccProject != null)
            //    //{
            //    //    projectName = _sccProvider.GetProjectFileName(sccProject);
            //    //}
            //}

            //var xxx = pProject as Project;

            //var solution = this.ServiceProvider.GetService(typeof(SVsSolution)) as Solution2;
            //var projItem = solution.FindProjectItem(rgszMkOldNames[0]);



            //Trace.WriteLine("OnQueryRenameFiles pProject:" + pProject + " old[0]:" + rgszMkOldNames[0] + " new[0]:" + rgszMkNewNames[0]);

            //pSummaryResult[0] = VSQUERYRENAMEFILERESULTS.VSQUERYRENAMEFILERESULTS_RenameOK;
            //if (rgResults != null)
            //{
            //    for (int iFile = 0; iFile < cFiles; iFile++)
            //    {
            //        rgResults[iFile] = VSQUERYRENAMEFILERESULTS.VSQUERYRENAMEFILERESULTS_RenameOK;
            //    }
            //}

            var res = VSConstants.E_NOTIMPL;
            return res;
        }

        public int OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {

            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames,
            VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
            VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        private void DocumentEventsOnDocumentSaved(Document document)
        {
            _invoker.GetOutputPaneWriteFunction()("DocumentEventsOnDocumentSaved doc:" + document.FullName);
        }

        public int OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags,
            VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            var msg = "OnQueryRemoveFiles pProject:" + pProject + " file[0]:" + rgpszMkDocuments[0];
            _invoker.GetOutputPaneWriteFunction()(msg);

            //per non farlo proseguire, mettere su RemoveNotOK
            //pSummaryResult[0] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
            //if (rgResults != null)
            //{
            //    for (int iFile = 0; iFile < cFiles; iFile++)
            //    {
            //        rgResults[iFile] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
            //    }
            //}


            //int iFound = 0;
            //uint itemId = 0;
            //VSDOCUMENTPRIORITY[] pdwPriority = new VSDOCUMENTPRIORITY[1];
            //int result = pProject.IsDocumentInProject(rgpszMkDocuments[0], out iFound, pdwPriority, out itemId);
            //if (result != VSConstants.S_OK)
            //    throw new Exception("Unexpected error calling IVsProject.IsDocumentInProject");
            //if (iFound == 0)
            //    throw new Exception("Cannot retrieve ProjectItem for template file");
            //if (itemId == 0)
            //    throw new Exception("Cannot retrieve ProjectItem for template file");


            //Microsoft.VisualStudio.OLE.Interop.IServiceProvider itemContext = null;
            //result = pProject.GetItemContext(itemId, out itemContext);
            //if (result != VSConstants.S_OK)
            //    throw new Exception("Unexpected error calling IVsProject.GetItemContext");
            //if (itemContext == null)
            //    throw new Exception("IVsProject.GetItemContext returned null");

            //ServiceProvider itemContextService = new ServiceProvider(itemContext);
            //EnvDTE.ProjectItem templateItem = (EnvDTE.ProjectItem)itemContextService.GetService(typeof(EnvDTE.ProjectItem));

            //var fullPath = templateItem.Properties.Item("FullPath").Value as string;

            //var sc = templateItem.DTE.SourceControl;

            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
            VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
            return VSConstants.E_NOTIMPL;
        }

    }
}
