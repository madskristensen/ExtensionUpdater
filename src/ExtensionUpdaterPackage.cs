using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.ExtensionUpdater
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [InstalledProductRegistration("#110", "#112", Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidExtensionUpdaterPkgString)]
    public sealed class ExtensionUpdaterPackage : ExtensionPointPackage
    {
        public const string Version = "1.2";

        protected override void Initialize()
        {
            base.Initialize();

            var repository = (IVsExtensionRepository)GetService(typeof(SVsExtensionRepository));
            var manager = (IVsExtensionManager)GetService(typeof(SVsExtensionManager));
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (repository == null || manager == null || mcs == null)
                return;

            Settings.Initialize(this);

            // Setup the menu buttons
            Commands commands = new Commands(repository, manager, mcs);
            commands.Initialize();

            // Check for extension updates
            Updater updater = new Updater(repository, manager);
            updater.CheckForUpdates();
        }
    }
}
