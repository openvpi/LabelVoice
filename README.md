# LabelVoice Dev Note

## 2022/10/2 added wav plot:
- Note: As for now, the wav plot needs some extra setup on Linux and Mac, see: https://scottplot.net/faq/dependencies/.
In `App.axaml.cs`:
```
String mainWindow = "main";
// String mainWindow = "wav";
if (mainWindow == "main")
{
    desktop.MainWindow = new MainWindow
    {
        DataContext = new MainWindowViewModel(),
    };
}
else if (mainWindow == "wav")
{
    desktop.MainWindow = new WavPlotWindow();
}
```
If `mainWindow == "wav"`, the wav plot window will be loaded.


## Build Instructions

### FFmpeg

If you're using `FFmpegWaveProvider` or other components related to FFmpeg, you should specify a directory containing all required shared libraries of FFmpeg for `FFmpeg.AutoGen` to call, then register the path at the program entry by setting `ffmpeg.RootPath`. The following libraries are mandatorily required.

+ avformat
+ avcodec
+ avutil
+ swresample

Alternatively, you can just place all required libraries at the same directory as the application.

You can also call `FFmpegWaveProvider.LibrariesExists()` to check if the shared libraries are available.

Get FFmpeg binary release at https://github.com/wang-bin/avbuild.
