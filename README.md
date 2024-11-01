# Window swircher/previewer

Half-baked bodged tool, because I needed it working, didn't find good alternatives, and didn't bother to make it from scratch.

I found a tool that worked great for one app and added a config value to change the process name that it searches.

Here is the [original README](ORIGINAL_README.md).

## Limitations

It does not work with all apps. The app must have:
* Separate processes for separate windows. For example, works with Paint, but does not work with Chromium browsers.
* Each window should have different title. That's how this tool remembers position of previews/thumbnails.
    * Although, you may use some window renamer tool for that, or launch application via `.bat` file and `start` command.

## Instruction

1. Open and close the app, this will create a config file.
2. Inside the config file, change `ProcessName` to the process name of the app you want to search.
    * Process name - is the name of `.exe` file without the `.exe` extension itself.
    * Defaullt value - `ExeFile`, as it was originaly intended for EVE Online.
    * You can edit config file only when the app is closed, otherwise it will not save.
4. Launch the app once again.
