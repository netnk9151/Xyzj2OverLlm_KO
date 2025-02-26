using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace Translate.Tests;

public class AssetTests 
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public void ExportTextAssets()
    {
        var guiAssets = "G:\\SteamLibrary\\steamapps\\common\\下一站江湖Ⅱ\\下一站江湖Ⅱ\\下一站江湖Ⅱ_Data\\StreamingAssets\\Gui\\gui.assetbundle";
        var exportPath = $"{workingDirectory}/Raw/Assets";
        AssetBundleExporter.ExportMonobehavioursWithText(guiAssets, exportPath);

    }
}