using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;
using System.Collections.Generic;


namespace WindowsGSM.Plugins
{
    public class AssettoCorsa : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.AssettoCorsa", // WindowsGSM.XXXX
            author = "ohmcodes",
            description = "WindowsGSM plugin for supporting Assetto Corsa Dedicated Server",
            version = "1.0",
            url = "https://github.com/ohmcodes/WindowsGSM.AssettoCorsa", // Github repository link (Best practice)
            color = "#E11212" // Color Hex
        };

        // - Standard Constructor and properties
        public AssettoCorsa(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => false;
        public override string AppId => "302550"; /* taken via https://steamdb.info/app/302550/info/ */

        // - Game server Fixed variables
        public override string StartPath => "acServer.exe"; // Game server start path
        public string FullName = "Assetto Corsa Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 0; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

        // - Game server default values
        public string ServerName = "wgsm_ac_dedicated";
        public string Defaultmap = "magione"; // Original (MapName)
        public string Maxplayers = "18"; // WGSM reads this as string but originally it is number or int (MaxPlayers)
        public string Port = "9600"; // WGSM reads this as string but originally it is number or int
        public string QueryPort = "9601"; // WGSM reads this as string but originally it is number or int (SteamQueryPort)
        public string Additional = string.Empty;


        private Dictionary<string, Dictionary<string, string>> configData;
        string filePath = string.Empty;

        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            filePath = Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), "cfg", "server_cfg.ini");

            createConfigFile();
            _serverData.EmbedConsole = true;
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            string param = string.Empty;

            // Modify CFG before start
            createConfigFile();

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
            try
            {
                p.Start();
                if (AllowsEmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }

                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
            });
            await Task.Delay(20000);
        }

        // - Update server function
        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            // old
            //var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            //Error = error;
            //await Task.Run(() => { p.WaitForExit(); });
            //return p;

            // Prepare Process
            string param = Installer.SteamCMD.GetParameter(ServerPath.GetServersServerFiles(_serverData.ServerID), AppId, true, loginAnonymous, null, custom);

            if (param == null)
            {
                Error += "Steam account not set up\n";
                return null;
            }

            string steamPath = ServerPath.GetBin("steamcmd", "steamcmd.exe");

            var p = new Process()
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetBin("steamcmd"),
                    FileName = steamPath,
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Fix the SteamCMD issue
            Directory.CreateDirectory(Path.Combine(ServerPath.GetServersServerFiles(_serverData.ServerID), "steamapps"));

            //if (AllowsEmbedConsole)
            //{
                //p.StartInfo.CreateNoWindow = false;
                //p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                //p.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                //p.StartInfo.RedirectStandardInput = true;
                //p.StartInfo.RedirectStandardOutput = true;
                //p.StartInfo.RedirectStandardError = true;
                //var serverConsole = new ServerConsole(_serverData.ServerID);
                //p.OutputDataReceived += serverConsole.AddOutput;
                //p.ErrorDataReceived += serverConsole.AddOutput;
            //}

            // Start Process
            try
            {
                p.Start();
                //if (AllowsEmbedConsole)
                //{
                //    p.BeginOutputReadLine();
                //    p.BeginErrorReadLine();
                //}

                return p;
            }
            catch (Exception e)
            {
                Error += e.Message + "\n";
                return null; // return null if fail to start
            }
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, "PackageInfo.bin");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }

        private void createConfigFile()
        {
            configData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            IniLoad();

            // Get a value
            //string serverName = IniGetValue("SERVER", "NAME");

            // Modify a value
            IniSetValue("SERVER", "NAME", _serverData.ServerName);
            IniSetValue("SERVER", "TRACK", _serverData.ServerMap);
            IniSetValue("SERVER", "TCP_PORT", _serverData.ServerPort);
            IniSetValue("SERVER", "UDP_PORT", _serverData.ServerPort);
            IniSetValue("SERVER", "MAX_CLIENTS", _serverData.ServerMaxPlayer);

            // Save the changes
            IniSave();
        }

        private void IniLoad()
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string currentSection = null;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        configData[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    else if (!string.IsNullOrEmpty(currentSection))
                    {
                        string[] parts = trimmedLine.Split(new[] { '=' }, 2);

                        if (parts.Length == 2)
                        {
                            configData[currentSection][parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error = $"Error reading config file: {ex.Message}";
            }
        }

        public string IniGetValue(string section, string key)
        {
            if (configData.ContainsKey(section) && configData[section].ContainsKey(key))
            {
                return configData[section][key];
            }
            return null;
        }

        public void IniSetValue(string section, string key, string value)
        {
            if (!configData.ContainsKey(section))
            {
                configData[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            configData[section][key] = value;
        }

        public void IniSave()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (var section in configData.Keys)
                    {
                        writer.WriteLine($"[{section}]");

                        foreach (var keyValue in configData[section])
                        {
                            writer.WriteLine($"{keyValue.Key}={keyValue.Value}");
                        }

                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Error = $"Error saving config file: {ex.Message}";
            }
        }
    }
}
