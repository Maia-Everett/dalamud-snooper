using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.Utility;

namespace Snooper;

internal class PluginUtils
{
	internal static string GetDefaultLogDirectory()
    {
        string subdirName = "Snooper Logs";

        if (Util.GetHostPlatform() == OSPlatform.Linux)
        {
            string? xdgDocumentsDir = GetXdgDocumentsDirectory();

            if (!string.IsNullOrEmpty(xdgDocumentsDir))
            {
                return xdgDocumentsDir + "/" + subdirName;
            }
        }
        
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), subdirName);
    }	

	private static string? GetXdgDocumentsDirectory()
    {
        try
        {
			// Wine hides the HOME environment variable from Windows processes,
			// so we ask the Linux kernel (via /proc) for the environment directly
            Dictionary<string, string> environment = GetLinuxEnvironment();
            string home = environment["HOME"];

			if (home.IsNullOrEmpty())
			{
				return null;
			}

            string xdgConfigHome = environment.GetValueOrDefault("XDG_CONFIG_HOME", home + "/.config");
            string userDirsConfig = xdgConfigHome + "/user-dirs.dirs";

            using (var reader = new StreamReader(userDirsConfig))
            {
                string? line;
                var regex = new Regex("^XDG_DOCUMENTS_DIR=\"(.+)\"$");

                while ((line = reader.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        return Regex.Replace(match.Groups[1].Value, @"^\$HOME/", home + "/");
                    }
                }

                return null;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

	private static Dictionary<string, string> GetLinuxEnvironment()
	{
		var environment = new Dictionary<string, string>();
            
		foreach (var line in File.ReadAllText("/proc/self/environ").Split('\0'))
		{
			var eqIndex = line.IndexOf('=');

			if (eqIndex != -1)
			{
				environment.Add(line[..eqIndex], line[(eqIndex + 1)..]);
			}
		}

		return environment;
	}
}
