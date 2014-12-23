using System;
using System.Linq;
using Microsoft.VisualStudio.ExtensionManager;

namespace MadsKristensen.ExtensionUpdater
{
    class UpdateChecker
    {
        private readonly IVsExtensionRepository _repository;
        private readonly IVsExtensionManager _manager;

        public UpdateChecker(IVsExtensionRepository extensionRepository, IVsExtensionManager extensionManager)
        {
            _manager = extensionManager;
            _repository = extensionRepository;
        }

        public bool CheckForUpdate(IInstalledExtension extension, out IInstallableExtension update)
        {
            // Find the vsix on the vs gallery
            // IMPORTANT: The .AsEnumerble() call is REQUIRED. Don't remove it or the update service won't work.
            GalleryEntry entry = _repository.CreateQuery<GalleryEntry>(includeTypeInQuery: false, includeSkuInQuery: true, searchSource: "ExtensionManagerUpdate")
                                            .Where(e => e.VsixID == extension.Header.Identifier)
                                            .AsEnumerable()
                                            .FirstOrDefault();

            // If we're running an older version then update
            if (entry != null && entry.NonNullVsixVersion > extension.Header.Version)
            {
                update = _repository.Download(entry);
                return true;
            }
            else
            {
                update = null;
                return false;
            }
        }

    }
}
