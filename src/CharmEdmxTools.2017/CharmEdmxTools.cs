using System;
using System.ComponentModel.Design;
using System.Globalization;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CharmEdmxTools
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CharmEdmxTools
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("24eddcbf-66c1-4625-abbf-db70214dc16a");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;


        private EdmxFixInvoker invoker;
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

            this.package = package;

            invoker = new EdmxFixInvoker(package);

            if (invoker._dte2 == null)
            {
                return;
            }

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                //var menuCommandID = new CommandID(CommandSet, CommandId);
                //var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                //commandService.AddCommand(menuItem);
                var menuCommandID1 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxExecAllFixs);
                var menuItem1 = new OleMenuCommand(EdmxContextMenuItemCallback, null, invoker.OnOptimizeContextBeforeQueryStatus, menuCommandID1);
                commandService.AddCommand(menuItem1);

                var menuCommandID2 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)PkgCmdIDList.cmdidEdmxClearAllProperties);
                var menuItem2 = new OleMenuCommand(EdmxContextMenuItemCallback, null, invoker.OnOptimizeContextBeforeQueryStatus, menuCommandID2);
                commandService.AddCommand(menuItem2);


                var menuCommandID3 = new CommandID(GuidList.guidCharmEdmxToolsCmdSet, (int)0x0025);
                var menuItem3 = new OleMenuCommand(EdmxMenuToolbarItemCallback, null, invoker.OnOptimizeMenuToolbarBeforeQueryStatus, menuCommandID3);
                commandService.AddCommand(menuItem3);
            }
        }


        //DTE2 _dte2;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CharmEdmxTools Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new CharmEdmxTools(package);
        }
        
        private void EdmxMenuToolbarItemCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            if (menuCommand == null)
                return;
            if (invoker._dte2.ActiveDocument == null)
                return;
            var selectedItem = invoker._dte2.ActiveDocument.ProjectItem;
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

            if (invoker._dte2.SelectedItems.Count != 1)
                return;

            var selectedItem = invoker._dte2.SelectedItems.Item(1).ProjectItem;
            var id = menuCommand.CommandID.ID;
            invoker.ExecEdmxFix(selectedItem, id);
        }
    }
}
