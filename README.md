# LabelVoice Dev Note

## Specifications & Designs

Specifications are at the `docs/` folder (only Chinese version for now).

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
