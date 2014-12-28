using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ExtensionManager;

namespace MadsKristensen.ExtensionUpdater
{
    class Updater
    {
        private readonly IVsExtensionRepository _repository;
        private readonly IVsExtensionManager _manager;
        private readonly UpdateChecker _checker;

        public Updater(IVsExtensionRepository extensionRepository, IVsExtensionManager extensionManager)
        {
            _manager = extensionManager;
            _repository = extensionRepository;
            _checker = new UpdateChecker(extensionRepository, extensionManager);
        }

        public void CheckForUpdates()
        {
            Task.Run(() => { Update(); });
        }

        private void Update()
        {
            try
            {
                DownloadAndInstall();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void DownloadAndInstall()
        {
            IEnumerable<IInstalledExtension> extensions = _manager.GetEnabledExtensions();

            foreach (IInstalledExtension extension in extensions)
            {
                if (!Settings.IsEnabled(extension.Header.Identifier))
                    continue;

                IInstallableExtension update;
                bool updateAvailable = _checker.CheckForUpdate(extension, out update);

                if (updateAvailable && update != null)
                {
                    _manager.Disable(extension);
                    _manager.Uninstall(extension);
                    _manager.InstallAsync(update, false);
                }
            }
        }
    }
}
