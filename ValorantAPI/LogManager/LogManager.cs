using System.Diagnostics.CodeAnalysis;
using System.IO;
using CyphersWatchfulEye.InternalLogic;
using CyphersWatchfulEye.ValorantAPI.Methods;
using Path = System.IO.Path;

namespace CyphersWatchfulEye.ValorantAPI.LogManager
{
    public class LogManager
    {
        public LogManagerEvents Events { get; private set; }
        public ClientData ClientData = null!;
        internal NetHandler Net;
        internal string CurrentLogText;
        internal string? LogPath;

        public LogManager(NetHandler net)
        {
            InitializePaths();
            Net = net;
            CurrentLogText = GetLogText();
            InitializeClientData();
            Events = new LogManagerEvents(ClientData, net);
            MonitorLogFile();
        }

        private void InitializePaths()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            LogPath = Path.Combine(userProfile, "AppData", "Local", "Valorant", "Saved", "Logs", "ShooterGame.log");
        }

        private void InitializeClientData()
        {
            string userId = MiscLogic.ExtractValue(CurrentLogText, "Logged in user changed: (.+)", 1);
            string pdUrl = MiscLogic.ExtractValue(CurrentLogText, @"https://pd\.[^\s]+\.net/", 0);
            string glzUrl = MiscLogic.ExtractValue(CurrentLogText, @"https://glz[^\s]+\.net/", 0);
            string regionData = MiscLogic.ExtractValue(CurrentLogText, @"https://pd\.([^\.]+)\.a\.pvp\.net/", 1);

            if (!Enum.TryParse(regionData, out ClientData.RegionCode region)) return;

            string sharedUrl = $"https://shared.{regionData}.a.pvp.net";
            ClientData = new ClientData(region, userId, pdUrl, glzUrl, sharedUrl);
        }

        public string GetLogText()
        {
            if (LogPath == null) return string.Empty;
            try
            {
                File.Copy(LogPath, $"{LogPath}.tmp", true);
                using StreamReader reader = File.OpenText($"{LogPath}.tmp");
                return reader.ReadToEnd();
            }
            catch
            {
                return GetLogText();
            }
            finally
            {
                File.Delete($"{LogPath}.tmp");
            }
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        private void MonitorLogFile()
        {
            long lastFileSize = 0;
            Task.Run(async () =>
            {
                for (;;)
                {
                    if (LogPath == null) continue;
                    await Task.Delay(100);
                    long currentFileSize = new FileInfo(LogPath).Length;
                    if (currentFileSize == lastFileSize) continue;
                    lastFileSize = currentFileSize;
                    Events?.ParseLogText(GetLogText());
                }
            });
        }
    }
}
