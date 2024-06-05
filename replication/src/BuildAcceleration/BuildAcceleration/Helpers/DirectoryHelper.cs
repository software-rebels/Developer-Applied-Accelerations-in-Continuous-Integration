using System;
using System.IO;

namespace ForecastBuildTime.Helpers;
internal class DirectoryHelper
{
    public string GetOutputDirectory()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var path = Path.Combine(desktop, "build_accel");
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetFullPath(string path)
    {
        return Path.Combine(GetOutputDirectory(), path);
    }
}
