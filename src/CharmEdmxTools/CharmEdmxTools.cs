using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    public sealed class CharmEdmxTools : IDisposable, IVsTrackProjectDocumentsEvents2, IVsRunningDocTableEvents3
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
            documentEvents.DocumentOpened += DocumentEventsOnDocumentOpened;
            documentEvents.DocumentClosing += DocumentEventsOnDocumentClosing;
            documentEvents.DocumentSaved += DocumentEventsOnDocumentSaved;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                commandEvents = events.CommandEvents;
                commandEvents.BeforeExecute += CommandEventsOnBeforeExecute;
            }
            //events.TextEditorEvents.LineChanged += TextEditorEventsOnLineChanged;
            //events.SolutionItemsEvents.ItemRemoved += SolutionItemsEventsOnItemRemoved;
            //var doctracker = (IVsRunningDocTableEvents3)this.ServiceProvider.GetService(typeof(SVsTrackProjectDocuments));

            m_RDT = (IVsRunningDocumentTable)this.ServiceProvider.GetService(typeof(SVsRunningDocumentTable));
            m_RDT.AdviseRunningDocTableEvents(this, out m_rdtCookie);
        }
        IVsRunningDocumentTable m_RDT;
        uint m_rdtCookie = 0;



        private void CommandEventsOnBeforeExecute(string guid, int i, object customIn, object customOut, ref bool cancelDefault)
        {
            /*T4: {1496A755-94DE-11D0-8C3F-00C04FC2AAE2} - id:1117 - name:Project.RunCustomTool*/
            if (i == 1627 || i == 1990 || i == 684 || i == 1628)
                return;
            try
            {
                var cmd = this._invoker._dte2.Commands.Item(guid, i);
                if (cmd == null)
                    return;
                _invoker.GetOutputPaneWriteFunction(focus: false)("CommandEventsOnBeforeExecute doc:" + guid + " - id:" + i +
                                                      " - name:" + cmd.Name);
            }
            catch (Exception)
            {

            }
        }

        public void Dispose()
        {
            if (m_RDT != null)
            {
                m_RDT.UnadviseRunningDocTableEvents(m_rdtCookie);
                m_RDT = null;
            }
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
        private CommandEvents commandEvents;

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

            var selectedItem = _invoker._dte2.SelectedItems.Item(1).ProjectItem;
            var id = menuCommand.CommandID.ID;
            _invoker.ExecEdmxFix(selectedItem, null, id);
        }

        #region Eventi Apertura/chisura documento
        private void DocumentEventsOnDocumentOpened(Document document)
        {
            if (document.FullName.EndsWith(FileExtensions.EntityDataModel, StringComparison.OrdinalIgnoreCase))
                edmxOpened.AddOrUpdate(document.FullName, s => document, (s, document1) => document);
        }

        private void DocumentEventsOnDocumentClosing(Document document)
        {
            DocumentEventsOnDocumentSaved(document);
            Document it;
            edmxOpened.TryRemove(document.FullName, out it);
        }
        #endregion

        #region Eventi salvataggio documento

        int IVsRunningDocTableEvents3.OnBeforeSave(uint docCookie)
        {
            if (edmxOpened.Count == 0)
                return VSConstants.S_OK;

            uint flags, readlocks, editlocks;
            string name; IVsHierarchy hier;
            uint itemid; IntPtr docData;
            m_RDT.GetDocumentInfo(docCookie, out flags, out readlocks, out editlocks, out name, out hier, out itemid, out docData);

            if (name == null)
                return VSConstants.S_OK;

            if (!name.EndsWith(FileExtensions.EntityDataModel, StringComparison.OrdinalIgnoreCase))
            {
                return VSConstants.S_OK;
            }

            var it = edmxSaving.AddOrUpdate(name, s => new SavingItemInfo(), (s, info) => new SavingItemInfo());
            it.Document = _invoker._dte2.Documents.OfType<Document>().SingleOrDefault(x => x.FullName == name);
            if (_invoker.Fixing == false)
            {
                string cfgCreated;
                var cfg = _invoker.GetConfigForItem(null, it.Document, false, out cfgCreated);
                if (cfg != null && cfg.AutoFixOnSave && cfgCreated == null)
                {
                    it.AutoFixed = _invoker.ExecAllFixsWithoutSave(it.Document);
                }
            }
            //_invoker.GetOutputPaneWriteFunction()("OnBeforeSave name:" + name);
            return VSConstants.S_OK;
        }

        private void DocumentEventsOnDocumentSaved(Document document)
        {
            if (document == null)
                return;
            SavingItemInfo it;
            if (edmxSaving.TryRemove(document.FullName, out it))
            {
                if (it.FilesNonEliminati != null)
                {
                    var logger = _invoker.GetOutputPaneWriteFunction();
                    logger("Ignore del delete dei seguenti files per fix SSC: " + string.Join(", ", it.FilesNonEliminati));
                }
                if (it.AutoFixed)
                {
                    var cont = File.ReadAllBytes(document.FullName);
                    File.WriteAllBytes(document.FullName, cont);
                }
            }
        }
        #endregion

        #region Eventi su files durante elaborazione T4


        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags,
            VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        {
            if (this.edmxSaving.Count == 0)
                return VSConstants.S_OK;

            //fix rimozione files: Avviene quando si elimina una colonna che fa parte di una Chiave
            for (var index = 0; index < rgpszMkDocuments.Length; index++)
            {
                var rgpszMkDocument = rgpszMkDocuments[index];
                var modelName = Path.GetFileNameWithoutExtension(rgpszMkDocument);
                var entityType = @"<EntityType Name=""" + modelName + @"""";
                var fileUsed = false;
                foreach (var savingItemInfo in edmxSaving)
                {
                    if (savingItemInfo.Value.XmlContent == null)
                    {
                        if (savingItemInfo.Value.Document == null)
                            savingItemInfo.Value.Document = _invoker._dte2.Documents.OfType<Document>()
                                .SingleOrDefault(x => x.FullName == savingItemInfo.Key);
                        savingItemInfo.Value.XmlContent = EdmxFixInvoker.GetDocumentText(savingItemInfo.Value.Document);
                        savingItemInfo.Value.Path = new FileInfo(savingItemInfo.Key).Directory.FullName;
                    }
                    if (!rgpszMkDocument.StartsWith(savingItemInfo.Value.Path))
                        continue;
                    if (savingItemInfo.Value.XmlContent.Contains(entityType))
                    {
                        fileUsed = true;
                        savingItemInfo.Value.FilesNonEliminati = savingItemInfo.Value.FilesNonEliminati ?? new List<string>();
                        savingItemInfo.Value.FilesNonEliminati.Add(Path.GetFileName(rgpszMkDocument));
                        break;
                    }
                }

                if (fileUsed)
                {
                    pSummaryResult[0] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                    if (rgResults != null)
                    {
                        rgResults[index] = VSQUERYREMOVEFILERESULTS.VSQUERYREMOVEFILERESULTS_RemoveNotOK;
                    }
                }
            }

            return VSConstants.S_OK;
        }

        #endregion

        #region variabili e utils

        private readonly ConcurrentDictionary<string, SavingItemInfo> edmxSaving = new ConcurrentDictionary<string, SavingItemInfo>();
        private readonly ConcurrentDictionary<string, Document> edmxOpened = new ConcurrentDictionary<string, Document>();

        private class SavingItemInfo
        {
            public string XmlContent { get; set; }
            public string Path { get; set; }
            public List<string> FilesNonEliminati { get; set; }
            public Document Document { get; set; }
            public bool AutoFixed { get; set; }
        }
        #endregion

        #region Eventi IVsTrackProjectDocumentsEvents2

        int IVsTrackProjectDocumentsEvents2.OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags,
            VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSADDFILEFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSREMOVEFILEFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames,
            VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEFILEFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames,
            VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
            VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments,
            VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult,
            VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices,
            string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion

        #region eventi IVsRunningDocTableEvents
        int IVsRunningDocTableEvents3.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld,
            string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents3.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining,
            uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents3.OnAfterSave(uint docCookie)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents3.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents3.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents3.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents3.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining,
            uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnAfterSave(uint docCookie)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld,
            string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents2.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining,
            uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.E_NOTIMPL;
        }
        #endregion
    }
}
