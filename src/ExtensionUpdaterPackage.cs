using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

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
        public const string Version = "1.0";

        protected override void Initialize()
        {
            base.Initialize();
            Settings.Initialize(this);

            var repo = GetService(typeof(SVsExtensionRepository)) as IVsExtensionRepository;
            var manager = GetService(typeof(SVsExtensionManager)) as IVsExtensionManager;

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            Commands commands = new Commands(repo, manager, mcs);
            commands.Initialize();
        }
    }
}
