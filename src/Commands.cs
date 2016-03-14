using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;

namespace MadsKristensen.ExtensionUpdater
{
    class Commands
    {
        private static bool _hasLoaded = false;
        private bool _isImportProcessing = false;
        private string[] _toInstallExtensions = new string[] { };

        private IVsExtensionRepository _repository;
        private IVsExtensionManager _manager;
        private OleMenuCommandService _mcs;
        private IVsOutputWindowPane _outputPane;

        public Commands(IVsExtensionRepository repo, IVsExtensionManager manager, OleMenuCommandService mcs, IVsOutputWindowPane outputPane)
        {
            _repository = repo;
            _manager = manager;
            _mcs = mcs;
            _outputPane = outputPane;
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
            
            CommandID importCommandID = new CommandID(GuidList.guidImportExportCmdSet, (int)PkgCmdIDList.cmdImport);
            OleMenuCommand import = new OleMenuCommand(Import, importCommandID);
            _mcs.AddCommand(import);

            CommandID exportCommandID = new CommandID(GuidList.guidImportExportCmdSet, (int)PkgCmdIDList.cmdExport);
            OleMenuCommand export = new OleMenuCommand(Export, exportCommandID);
            _mcs.AddCommand(export);
        }

        private void WriteToOutputPane(string message)
        {
            _outputPane.OutputString(message + Environment.NewLine);
            _outputPane.Activate();
        }

        #region Import / export
        
        private void Import(object sender, EventArgs e)
        {
            if (_isImportProcessing)
            {
                WriteToOutputPane("Extensions import ignored - one is currently already running.");
                return;
            }

            var openFileDialog = new OpenFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".vsextensionslist",
                CheckPathExists = true,
                Filter = "Extensions List (.vsextensionslist)|*.vsextensionslist",
                FilterIndex = 1,
                //InitialDirectory = "%userprofile%",
            };

            var userClickedOK = openFileDialog.ShowDialog() ?? false;
            if (!userClickedOK)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                WriteToOutputPane("Extensions not imported - please select a file.");
                return;
            }

            _isImportProcessing = true;
            WriteToOutputPane("Importing extensions...");

            string[] importFileLines = null;
            try
            {
                importFileLines = File.ReadAllLines(openFileDialog.FileName).Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToArray();
            }
            catch
            {
                WriteToOutputPane("Error accessing/reading import file.");
                _isImportProcessing = false;
                return;
            }

            if (!importFileLines.Any())
            {
                WriteToOutputPane("No extensions were found in the import file.");
                _isImportProcessing = false;
                return;
            }
            
            // Get extensions not already installed
            var _installedExtensions = Commands.GetExtensions(_manager).ToDictionary(ie => ie.Header.Identifier, ie => ie.Header.Name);
            _toInstallExtensions = importFileLines.Where(l => _installedExtensions.All(ie => ie.Key != l)).ToArray();

            if (!_toInstallExtensions.Any())
            {
                WriteToOutputPane("You've already got all the extensions listed in the import file.");
                _isImportProcessing = false;
                return;
            }

            // Query for the complete new extension objects
            var query = _repository.CreateQuery<GalleryEntry>(false, true, "ExtensionManagerQuery")
                .Where(entry => _toInstallExtensions.Contains(entry.VsixID))
                .OrderBy(entry => entry.Name)
                .Skip(0)
                .Take(500)
                 as IVsExtensionRepositoryQuery<GalleryEntry>;

            WriteToOutputPane(
                string.Format("Looking up {0} potentially new extension/s in the gallery after skipping {1} already installed extension/s...", 
                    _toInstallExtensions.Length, 
                    importFileLines.Length - _toInstallExtensions.Length
                )
            );

            query.ExecuteCompleted += Query_ExecuteCompleted;
            query.ExecuteAsync();
		}

        private void Query_ExecuteCompleted(object sender, ExecuteCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                WriteToOutputPane("Error looking up new extension/s in the gallery...");
                _isImportProcessing = false;
                return;
            }

            // Extract results of found extensions
            var foundExtensions =  e.Results.Cast<GalleryEntry>().ToArray();
            var installableExtensions = foundExtensions.Where(entry => _toInstallExtensions.Any(l => l != entry.VsixID)).ToArray();
            var missingExtensions = _toInstallExtensions.Except(foundExtensions.Select(fe => fe.VsixID)).ToArray();

            if (!installableExtensions.Any())
            {
                WriteToOutputPane("Couldn't find any of the new extension/s in the gallery.");
                _isImportProcessing = false;
                return;
            }

            if (missingExtensions.Any())
            {
                WriteToOutputPane("Couldn't find " + missingExtensions.Length + " of the new extension/s in the gallery.");
            }

            // Download and install the new ones
            WriteToOutputPane("Installing new extension/s:");
            var wasAnInstallSuccessful = false;
            foreach (var installableExtension in installableExtensions)
            {
                var msg = string.Format(" - '{0}' ", installableExtension.Name);

                IInstallableExtension extension = null;
                try
                {
                    extension = _repository.Download(installableExtension);
                    _manager.Install(extension, false);
                    msg += "installed.";
                    wasAnInstallSuccessful = true;
                }
                catch (Exception ex)
                {
                    msg += "install failed. " + ex.Message;
                }

                WriteToOutputPane(msg);
            }

            if (wasAnInstallSuccessful)
            {
                WriteToOutputPane(Environment.NewLine + Environment.NewLine + "Please restart for changes to take affect.");
            }

            WriteToOutputPane("Extensions imported."); 

            // Reset
            _isImportProcessing = false;
            _toInstallExtensions = new string[] { };
        }
                
        private void Export(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = ".vsextensionslist",
                CheckPathExists = true,
                Filter = "Extensions List (.vsextensionslist)|*.vsextensionslist",
                FilterIndex = 1,
                //InitialDirectory = "%userprofile%",
            };

            var userClickedOK = saveFileDialog.ShowDialog() ?? false;
            if (!userClickedOK)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(saveFileDialog.FileName))
            {
                WriteToOutputPane("Extensions not exported - please choose a filename.");
            }

            WriteToOutputPane("Exporting your extensions...");

            var installedExtensions = Commands.GetExtensions(_manager);
            var sbInstalledExtensions = new StringBuilder(installedExtensions.Count() * 50);
            foreach (var ext in installedExtensions)
            {
                sbInstalledExtensions.AppendLine(ext.Header.Identifier);
            }

            try
            {
                File.WriteAllText(saveFileDialog.FileName, sbInstalledExtensions.ToString());
                WriteToOutputPane("Extensions exported.");
            }
            catch
            {
                WriteToOutputPane("Problem exporting extensions.");
            }
        }

        #endregion
        
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
