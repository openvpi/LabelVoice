# LabelVoice Dev Note
## 2022/10/2 added wav plot:
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