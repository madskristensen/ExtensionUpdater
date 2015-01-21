using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.ExtensionManager;

namespace MadsKristensen.ExtensionUpdater.Dialog
{
	public partial class SearchDialog : Window
	{
		public IVsExtensionRepository _repository;
		public IVsExtensionManager _manager;
		private string lastSearch = "__||__";
		private bool _isProcessing = false;
		private static List<GalleryEntry> _cache = new List<GalleryEntry>();

		public SearchDialog()
		{
			InitializeComponent();
			PreviewKeyDown += new KeyEventHandler(HandleEsc);
			label.Content = string.Empty;
			comboBox.ItemsSource = _cache;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				comboBox.Focus();
				System.Windows.Forms.SendKeys.Send("{TAB}"); // Sets focus in the combobox

			}), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
		}

		private void OnTextChanged(object sender, TextChangedEventArgs e)
		{
			FilterDropDown();

			if (comboBox.Text.Trim().Length < 3 || comboBox.Text.Trim().StartsWith(lastSearch) || _isProcessing)
				return;

			_isProcessing = true;

			var query = _repository.CreateQuery<GalleryEntry>(false, true, "ExtensionManagerQuery")
				.OrderBy(entry => entry.Name)
				.Skip(0)
				.Take(500)
				 as IVsExtensionRepositoryQuery<GalleryEntry>;

			query.SearchText = comboBox.Text.Trim();
			query.ExecuteCompleted += Query_ExecuteCompleted;
			query.ExecuteAsync();

			lastSearch = comboBox.Text.Trim();
		}

		private void FilterDropDown()
		{
			comboBox.ItemsSource = _cache.ToArray();//.Where(entry => entry.Name.IndexOf(comboBox.Text, StringComparison.OrdinalIgnoreCase) > -1);
		}

		private void Query_ExecuteCompleted(object sender, ExecuteCompletedEventArgs e)
		{
			_isProcessing = false;
			if (e.Error != null)
				return;

			var installed = Commands.GetExtensions(_manager);
			var entries = e.Results.Cast<GalleryEntry>().Where(entry => !installed.Any(i => i.Header.Identifier == entry.VsixID));

			if (!entries.Any())
				return;

			_cache.AddRange(entries);

			comboBox.IsDropDownOpen = true;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				FilterDropDown();

			}), System.Windows.Threading.DispatcherPriority.Normal, null);

			button.IsEnabled = comboBox.SelectedIndex != -1;
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
			GalleryEntry selected = comboBox.SelectedItem as GalleryEntry;
			label.Content = "Installing the extension...";

			IInstallableExtension extension = _repository.Download(selected);
			_manager.Install(extension, false);

			label.Content = "Extension installed. Restart to load it.";
		}

		private void HandleEsc(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				Close();
		}
	}
}
