using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.ExtensionUpdater
{
    class Settings
    {
        private const string _name = "Extension Updater";
        private const string _identifierKey = "ExtensionIdentifiers";
        private const string _separator = "__|--";

        private static SettingsManager _manager;
        private static SettingsStore _readStore;
        private static WritableSettingsStore _writeStore;

        public static void Initialize(IServiceProvider provider)
        {
            _manager = new ShellSettingsManager(provider);
            _readStore = _manager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            _writeStore = _manager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!_writeStore.CollectionExists(_name))
            {
                _writeStore.CreateCollection(_name);

                // Add this package identifier to the initial list.
                _writeStore.SetString(_name, _identifierKey, GuidList.guidExtensionUpdaterPkgString);
            }
        }

        public static bool Enabled
        {
            get
            {
                return _readStore.GetBoolean(_name, "IsEnabled", true);
            }
            set
            {
                _writeStore.SetBoolean(_name, "IsEnabled", value);
            }
        }

        public static bool IsEnabled(string identifier)
        {
            string raw = _readStore.GetString(_name, _identifierKey, string.Empty);

            if (string.IsNullOrEmpty(raw))
                return false;

            string[] identifiers = raw.Split(new[] { _separator }, StringSplitOptions.RemoveEmptyEntries);

            return identifiers.Contains(identifier);
        }

        public static void ToggleEnabled(string identifier, bool isEnabled)
        {
            string raw = _readStore.GetString(_name, _identifierKey, string.Empty);

            IEnumerable<string> ids = raw.Split(new[] { _separator }, StringSplitOptions.RemoveEmptyEntries);

            if (isEnabled)
            {
                ids = ids.Union(new[] { identifier });
            }
            else
            {
                ids = ids.Where(i => i != identifier);
            }

            string newValue = string.Join(_separator, ids);
            _writeStore.SetString(_name, _identifierKey, newValue);
        }
    }
}
