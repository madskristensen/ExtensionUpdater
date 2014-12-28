using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Threading;

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
        public const string Version = "1.1";

        protected override void Initialize()
        {
            base.Initialize();

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                Settings.Initialize(this);

                var repository = (IVsExtensionRepository)GetService(typeof(SVsExtensionRepository));
                var manager = (IVsExtensionManager)GetService(typeof(SVsExtensionManager));

                // Setup the menu buttons
                OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                Commands commands = new Commands(repository, manager, mcs);
                commands.Initialize();

                // Check for extension updates
                Updater updater = new Updater(repository, manager);
                updater.CheckForUpdates();

            }), DispatcherPriority.ApplicationIdle, null);
        }
    }
}
