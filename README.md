## Visual Studio Auto Updater

This extension allows you to specify which of your Visual Studio extensions
you want to automatically update when a new version is released.

### How it works

Every time Visual Studio opens, a background process checks for updates
to the extensions you have specified for auto updating.

If it finds any, it will silently install them in the background,
so that next time you open Visual Studio, you'll have the latest versions
of your favorite extensions already installed.

You specify which extensions to auto update in a flyout menu located in the 
`Tools` menu.

![Screenshot](https://raw.githubusercontent.com/madskristensen/ExtensionUpdater/master/artifacts/screenshot.png)

Not all extensions are listed. Only the ones that can be auto updated, which excludes

1. Extensions installed by MSIs.
2. Extensions that require admin permissions to update.
3. Extensions that are shipped as part of Visual Studio.