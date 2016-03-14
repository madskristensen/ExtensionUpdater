using System;

namespace MadsKristensen.ExtensionUpdater
{
    static class GuidList
    {
        public const string guidExtensionUpdaterPkgString = "cb8900a0-79fb-4c76-a6d6-f4e88d3384d2";

        public const string guidExtensionUpdaterCmdSetString = "c1703d5f-c150-4977-a4a7-d4a6094412bf";
        public const string guidFlyoutMenuString = "c1703d5f-c150-4977-a4a2-d4a6094412ba";

        public const string guidImportExportCmdSetString = "c1703d5f-c150-4977-a4a7-d4a6094412be";
        public const string guidFlyoutImportExportMenuString = "c1703d5f-c150-4977-a4a2-d4a6094412bb";
        
        public static readonly Guid guidExtensionUpdaterCmdSet = new Guid(guidExtensionUpdaterCmdSetString);
        public static readonly Guid guidFlyoutMenu = new Guid(guidFlyoutMenuString);

        public static readonly Guid guidImportExportCmdSet = new Guid(guidImportExportCmdSetString);
        public static readonly Guid guidFlyoutImportExportMenu = new Guid(guidFlyoutImportExportMenuString);
    }

    static class PkgCmdIDList
    {
        public const uint cmdEnableAutoUpdate = 0x100;
        public const uint cmdCheckAll = 0x200;
        public const uint cmdSearch = 0x300;

        public const uint cmdImport = 0x400;
        public const uint cmdExport = 0x500;
    }
}
