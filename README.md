## Visual Studio Auto Updater

[![Build status](https://ci.appveyor.com/api/projects/status/61vaxxkrbdmtmql0?svg=true)](https://ci.appveyor.com/project/madskristensen/extensionupdater)

[Download the extension on the VS Gallery](https://visualstudiogallery.msdn.microsoft.com/14973bbb-8e00-4cab-a8b4-415a38d78615)

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

The first item in the menu is `Enable Automatic Updates`. That's the master switch for 
this feature. If it is unchecked, no auto updating will take place for any extension.

Not all extensions are listed. Only the ones that can be auto updated, which excludes

1. Extensions installed by MSIs.
2. Extensions that require admin permissions to update.
3. Extensions that are shipped as part of Visual Studio.

### Known issues

It only works in Visual Studoi 2015 due to technical issues. I'm trying to find a way to make it work. 
If you have any ideas or are curious as to why it doesn't work, 
please join the discussion on [the issue tracker](https://github.com/madskristensen/ExtensionUpdater/issues/1)