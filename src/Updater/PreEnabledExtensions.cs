
namespace MadsKristensen.ExtensionUpdater
{
    static class PreEnabledExtensions
    {
        /// <summary>
        ///  A list of extensions that are safe to auto update by default.
        /// </summary>
        /// <remarks>
        /// You can find the guid/ID for any extension by looking in the .vsixmanifest file.
        /// The ID is located in the <Identity Id="guid/id here" /> attribute.
        /// </remarks>
        public static string[] List = new string[]
        {
            GuidList.guidExtensionUpdaterPkgString, // This extension
            "4c1a78e6-e7b8-4aa9-8812-4836e051ff6d", // Trailing Whitespace Visualizer
            "27dd9dea-6dd2-403e-929d-3ff20d896c5e", // Add New File
            "6c799bc4-0d4c-4172-98bc-5d464b612dca", // File Nesting
            "aaa8d5c5-24d8-4c45-9620-9f77b2aa6363", // Package Intellisense
            "0798393f-f7b0-4283-a36e-c57a73f031c4", // Error Watcher
            "cced4e72-2f8c-4458-b8df-4934677e4bf3", // GruntLauncher
        };
    }
}