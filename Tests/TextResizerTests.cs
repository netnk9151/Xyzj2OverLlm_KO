using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Translate.Tests;

public class TextResizerTests
{

    const string workingDirectory = "../../../../Files";

    const int SYMBOLIC_LINK_FLAG_DIRECTORY = 1; // Used for directory symlinks

    [Fact]
    public void CreateSymlinkToResizer()
    {
        var inputFolder = $"{workingDirectory}/Resizers";
        inputFolder = Path.GetFullPath(inputFolder);
        var outputFolder = @"G:\SteamLibrary\steamapps\common\下一站江湖Ⅱ\下一站江湖Ⅱ\BepInEx\resizers";

        try
        {
            if (Directory.Exists(outputFolder))
            {
                Console.WriteLine("Output folder already exists. Deleting it...");
                Directory.Delete(outputFolder, true);
            }

            // Run mklink command to create a symbolic link
            string command = $"/C mklink /D \"{outputFolder}\" \"{inputFolder}\"";
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas" // Run as administrator
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Display output or error
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine("Success: " + output);
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine("Error: " + error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
