using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
//using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CharmEdmxTools
{
    /// <summary>
    /// Command handler
    /// </summary>
    public sealed class CharmEdmxTools
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
            Instance = new CharmEdmxTools(package);
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
        }


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
    }
}
