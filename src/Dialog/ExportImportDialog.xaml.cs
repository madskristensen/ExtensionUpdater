using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.ExtensionManager;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace MadsKristensen.ExtensionUpdater.Dialog
{
    public partial class ExportImportDialog : Window
    {
        public IVsExtensionRepository _repository;
        public IVsExtensionManager _manager;

        private bool _isExportTextBoxProcessing = false;
        private bool _isImportTextBoxProcessing = false;

        private bool _isImportProcessing = false;
        private string[] _newExtensionsCache = new string[] { };

        public ExportImportDialog()
        {
            InitializeComponent();
            PreviewKeyDown += new KeyEventHandler(HandleEsc);
            textBlock.Text = string.Empty;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.Forms.SendKeys.Send("{TAB}"); // Sets focus in the textbox

            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
        }
        
        private void exportButton_Click(object sender, RoutedEventArgs e)
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
                textBlock.Text = "Extensions not exported - please choose a filename.";
            }

            textBlock.Text = "Exporting your extensions...";

            var installedExtensions = Commands.GetExtensions(_manager);
            var sbInstalledExtensions = new StringBuilder(installedExtensions.Count() * 50);
            foreach (var ext in installedExtensions)
            {
                sbInstalledExtensions.AppendLine(ext.Header.Identifier);
            }

            try
            {
                File.WriteAllText(saveFileDialog.FileName, sbInstalledExtensions.ToString());
                textBlock.Text = "Extensions exported.";
            }
            catch
            {
                textBlock.Text = "Problem exporting extensions.";
            }
        }
        
        private void importButton_Click(object sender, RoutedEventArgs e)
        {
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
                textBlock.Text = "Extensions not imported - please select a file.";
            }

            _isImportProcessing = true;
            textBlock.Text = "Importing your extensions...";

            // Get lines from file
            string[] importFileLines = null;
            try
            {
                importFileLines = File.ReadAllLines(openFileDialog.FileName).Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToArray();
            }
            catch
            {
                textBlock.Text = "Error accessing/reading import file.";
                _isImportProcessing = false;
                return;
            }

            // Validate 
            if (!importFileLines.Any())
            {
                textBlock.Text = "No extensions were found in the import file.";
                _isImportProcessing = false;
                return;
            }

            // Get extensions not already installed
            var installedExtensions = Commands.GetExtensions(_manager);
            var newExtensions = importFileLines.Where(l => installedExtensions.All(ie => ie.Header.Identifier != l));
            var _newExtensionsCache = newExtensions.ToArray();

            if (!_newExtensionsCache.Any())
            {
                textBlock.Text = "Extensions were found, but you've already got them all.";
                _isImportProcessing = false;
                return;
            }

            // Query for the complete new extension objects
            var query = _repository.CreateQuery<GalleryEntry>(false, true, "ExtensionManagerQuery")
                .Where(entry => _newExtensionsCache.Contains(entry.VsixID))
                .OrderBy(entry => entry.Name)
                .Skip(0)
                .Take(500)
                 as IVsExtensionRepositoryQuery<GalleryEntry>;

            textBlock.Text = "Looking up new extensions in the gallery...";

            query.ExecuteCompleted += Query_ExecuteCompleted;
            query.ExecuteAsync();
        }

        private void Query_ExecuteCompleted(object sender, ExecuteCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                textBlock.Text = "Error looking up new extensions in the gallery...";
                _isImportProcessing = false;
                return;
            }

            var installed = Commands.GetExtensions(_manager);
            var entries = e.Results.Cast<GalleryEntry>().Where(entry => installed.All(i => i.Header.Identifier != entry.VsixID));

            if (!entries.Any())
            {
                textBlock.Text = "Couldn't find any of the new extensions in the gallery.";
                // TODO: Persist _newExtensionsCache so available on callback
                //+ "Specifically:\r\n" + String.Join("\r\n", _newExtensionsCache.Select(id => " - " + id));
                _isImportProcessing = false;
                return;
            }

            // Download and install the new ones
            try
            {
                var sbInstallReport = new StringBuilder("Installed new extensions:\r\n", entries.Count() * 50);
                var wasAnInstallSuccessful = false;
                foreach (var entry in entries)
                {
                    sbInstallReport.AppendFormat(" - '{0}' ", entry.Name);

                    IInstallableExtension extension = null;
                    try
                    {
                        extension = _repository.Download(entry);
                        _manager.Install(extension, false);
                        sbInstallReport.AppendLine("installed.");
                        wasAnInstallSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        sbInstallReport.AppendLine("install failed. " + ex.Message);
                    }
                }

                if (wasAnInstallSuccessful)
                {
                    sbInstallReport.AppendLine().AppendLine("Please restart for changes to take affect.");
                }

                textBlock.Text = sbInstallReport.ToString();
            }
            catch (Exception ex)
            {
                textBlock.Text = "Problem dowloading/installing extension/s.\r\nException message: " + ex.Message;
            }
            finally
            {
                _isImportProcessing = false;
            }
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
