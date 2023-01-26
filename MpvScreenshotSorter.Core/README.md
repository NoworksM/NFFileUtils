# MpvScreenshotSorter.Core

MpvScreenshotSorter.Core is a simple program to automate the sorting of screenshots mainly for anime files. Mostly done to replace an old Python script I had to do the same
thing for stuff I'm watching on my computer.

| Argument                                     | Default Value          | Description                                                                                                           |
|----------------------------------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------|
| `-w`, `--watch`                              | Off                    | Run the program in watch mode so it sorts the initial files and then sorts any new ones that come in                  |
| `-v`, `--verbose`                            | Off                    | Enable verbose logging                                                                                                |
| `-d`, `--debug`                              | Off                    | Enable debug mode which doesn't move any files                                                                        |
| `-a`, `--log-to-config`, `--log-to-app-data` | Off                    | Log data to the app data/config (%APPDATA%/fshorter, $HOME/.config/fshorter) folder depending on the operating system |
| `-o`, `--output`                             | `..\{show}\{filename}` | Output root path for the new files. Files will be put in show subfolder inside it                                     |
| `[paths...]`                                 | No Default             | Paths to look for files in. Can be either a folder or a file                                                          |