using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.ExtensionUpdater
{
    class Commands
    {
        private static bool _hasLoaded = false;
        private IVsExtensionRepository _repository;
        private IVsExtensionManager _manager;
        private OleMenuCommandService _mcs;

        public Commands(IVsExtensionRepository repo, IVsExtensionManager manager, OleMenuCommandService mcs)
        {
            _repository = repo;
            _manager = manager;
            _mcs = mcs;
        }

        public void Initialize()
        {
            CommandID menuCommandID = new CommandID(GuidList.guidExtensionUpdaterCmdSet, (int)PkgCmdIDList.cmdEnableAutoUpdate);
            OleMenuCommand command = new OleMenuCommand(MasterSwitch, menuCommandID);
            command.BeforeQueryStatus += (s, e) => { SetVisibility(command); };
            _mcs.AddCommand(command);

            CommandID checkAllCommandID = new CommandID(GuidList.guidExtensionUpdaterCmdSet, (int)PkgCmdIDList.cmdCheckAll);
            OleMenuCommand checkAll = new OleMenuCommand(CheckAll, checkAllCommandID);
            _mcs.AddCommand(checkAll);

            //CommandID searchCommandID = new CommandID(GuidList.guidExtensionUpdaterCmdSet, (int)PkgCmdIDList.cmdSearch);
            //OleMenuCommand search = new OleMenuCommand(Search, searchCommandID);
            //_mcs.AddCommand(search);
		}

        //private void Search(object sender, EventArgs e)
        //{
        //    Dialog.SearchDialog searchbox = new Dialog.SearchDialog();
        //    searchbox._manager = _manager;
        //    searchbox._repository = _repository;
        //    searchbox.ShowActivated = true;
        //    searchbox.ShowDialog();
        //}

		private void CheckAll(object sender, EventArgs e)
        {
            foreach (var extension in GetExtensions(_manager))
            {
                Settings.ToggleEnabled(extension.Header.Identifier, true);
            }
        }

        private void MasterSwitch(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            Settings.Enabled = !command.Checked;

            if (!command.Checked) // Not checked means that it is checked.
            {
                Updater updater = new Updater(_repository, _manager);
                updater.CheckForUpdates();
            }
        }

        private void SetVisibility(OleMenuCommand master)
        {
            master.Checked = Settings.Enabled;

            if (_hasLoaded)
                return;

            int num = 0;

            foreach (var extension in GetExtensions(_manager).OrderBy(e => e.Header.Name))
            {
                num++;
                CommandID commandId = new CommandID(GuidList.guidExtensionUpdaterCmdSet, (int)PkgCmdIDList.cmdEnableAutoUpdate + num);
                OleMenuCommand command = PrepareMenuItem(extension, commandId);

                _mcs.AddCommand(command);
            }

            _hasLoaded = true;
        }

        private OleMenuCommand PrepareMenuItem(IInstalledExtension extension, CommandID commandId)
        {
            OleMenuCommand command = new OleMenuCommand(ToggleAutoUpdating, commandId);
            command.Text = extension.Header.Name;
            command.ParametersDescription = extension.Header.Identifier;
            command.Checked = Settings.IsEnabled(extension.Header.Identifier);
            command.BeforeQueryStatus += (x, y) =>
            {
                OleMenuCommand c = (OleMenuCommand)x;
                c.Enabled = Settings.Enabled;
                c.Checked = Settings.IsEnabled(c.ParametersDescription);
            };

            return command;
        }

        private void ToggleAutoUpdating(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            Settings.ToggleEnabled(command.ParametersDescription, !command.Checked);
        }

        public static IEnumerable<IInstalledExtension> GetExtensions(IVsExtensionManager manager)
        {
            return from e in manager.GetInstalledExtensions()
                   where !e.Header.SystemComponent && !e.Header.AllUsers && !e.Header.InstalledByMsi && e.State == EnabledState.Enabled
                   select e;
        }
    }
}
