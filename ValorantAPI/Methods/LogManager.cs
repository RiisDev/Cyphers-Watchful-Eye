using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using CyphersWatchfulEye.InternalLogic;
using CyphersWatchfulEye.ValorantAPI.DataTypes;
using Path = System.IO.Path;
#pragma warning disable IDE0057

namespace CyphersWatchfulEye.ValorantAPI.Methods
{
    public record ClientData(ClientData.RegionCode Region, string UserId, string PdUrl, string GlzUrl, string SharedUrl)
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        public enum RegionCode
        {
            na,
            latam,
            br,
            eu,
            ap,
            kr,
        }
    }

    public class LogManager
    {
        private string _lastInvoke = "";
        private long _lastIndex;

        internal enum PartyDataReturn
        {
            CustomGame,
            ChangeQueue
        }

        public ClientData ClientData;
        internal NetHandler Net;
        internal string CurrentLogText;
        internal string LogPath;

        public delegate void ValorantEvent(string? value);
        public event ValorantEvent OnQueueChanged = null!;
        public event ValorantEvent OnEnteredQueue = null!;
        public event ValorantEvent OnLeftQueue = null!;
        public event ValorantEvent OnPreGamePlayerLoaded = null!;
        public event ValorantEvent OnPreGameMatchLoaded = null!;
        public event ValorantEvent OnAgentLocked = null!;
        public event ValorantEvent OnSelectedCharacter = null!;
        public event ValorantEvent OnGameLoaded = null!;
        public event ValorantEvent OnGameEnded = null!;
        public event ValorantEvent OnCustomGameLobbyCreated = null!;

        public LogManager(NetHandler net)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            LogPath = Path.Combine(userProfile, "AppData", "Local", "Valorant", "Saved", "Logs", "ShooterGame.log");
            Net = net;
            CurrentLogText = GetLogText();
            string userId = MiscLogic.ExtractValue(CurrentLogText, "Logged in user changed: (.+)", 1);
            string pdUrl = MiscLogic.ExtractValue(CurrentLogText, @"https://pd\.[^\s]+\.net/", 0);
            string glzUrl = MiscLogic.ExtractValue(CurrentLogText, @"https://glz[^\s]+\.net/", 0);
            string regionData = MiscLogic.ExtractValue(CurrentLogText, @"https://pd\.([^\.]+)\.a\.pvp\.net/", 1);
            string sharedUrl = $"https://shared.{regionData}.a.pvp.net";
            _ = Enum.TryParse(regionData, out ClientData.RegionCode region);
            ClientData = new ClientData(region, userId, pdUrl, glzUrl, sharedUrl);
            MonitorLogFile();
        }

        public string GetLogText()
        {
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

        private static string GetEndpoint(string prefix, string log) => TryExtractSubstring(log, "https", ']', startIndex => startIndex != -1, prefix);
        private static string GetMapName(string log) => TryExtractSubstring(log, "Map Name:", '|', startIndex => startIndex >= 0).Trim();
        private static string GetWinningTeam(string log) => TryExtractSubstring(log, "Team: ", '\'', startIndex => startIndex >= 0).Trim();

        private static string TryExtractSubstring(string log, string startToken, char endToken, Func<int, bool> condition, string prefix = "")
        {
            int startIndex = log.IndexOf(startToken, StringComparison.Ordinal);
            int endIndex = log.IndexOf(endToken, startIndex);
            return (startIndex != -1 && endIndex != -1 && condition(startIndex)) ? log.Substring(startIndex, endIndex - startIndex).Replace(prefix, "") : "";
        }

        private async Task<string?> GetPartyData(PartyDataReturn dataReturn, string endPoint)
        {
            string? data = await Net.GetAsync(ClientData.GlzUrl, endPoint);

            return data is not null ? dataReturn switch
            {
                PartyDataReturn.CustomGame => JsonSerializer.Deserialize<PartyInfo>(data)?.CustomGameData
                    .ToString(),
                PartyDataReturn.ChangeQueue => JsonSerializer.Deserialize<ChangeQueue>(data)?.MatchmakingData.QueueID,
                _ => null,
            } : null;
        }

        private async void ParseLogText(string logText)
        {
            string[] fileLines = logText.Split('\n');

            for (long lineIndex = fileLines.Length-1; lineIndex > _lastIndex; lineIndex--)
            {
                string agentId;
                string line = fileLines[lineIndex];

                if (_lastIndex == lineIndex) break;

                switch (line)
                {
                    case var _ when line.Contains("Party_ChangeQueue"):
                        OnQueueChanged?.Invoke(await GetPartyData(PartyDataReturn.ChangeQueue, GetEndpoint(ClientData.GlzUrl, line).Replace("/queue", "")));
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Party_EnterMatchmakingQueue"):
                        if (_lastInvoke == "Party_EnterMatchmakingQueue") break;
                        OnEnteredQueue?.Invoke(await GetPartyData(PartyDataReturn.ChangeQueue, GetEndpoint(ClientData.GlzUrl, line)));
                        _lastInvoke = "Party_EnterMatchmakingQueue";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Party_LeaveMatchmakingQueue"):
                        if (_lastInvoke == "Party_LeaveMatchmakingQueue") break;
                        OnLeftQueue?.Invoke(await GetPartyData(PartyDataReturn.ChangeQueue, GetEndpoint(ClientData.GlzUrl, line)));
                        _lastInvoke = "Party_LeaveMatchmakingQueue";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Party_MakePartyIntoCustomGame"):
                        if (_lastInvoke == "Party_MakePartyIntoCustomGame") break;
                        OnCustomGameLobbyCreated?.Invoke(await GetPartyData(PartyDataReturn.CustomGame, GetEndpoint(ClientData.GlzUrl, line).Replace("/makecustomgame", "")));
                        _lastInvoke = "Party_MakePartyIntoCustomGame";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Pregame_GetPlayer"):
                        if (_lastInvoke == "Pregame_GetPlayer") break;
                        OnPreGamePlayerLoaded?.Invoke(ClientData.UserId);
                        _lastInvoke = "Pregame_GetPlayer";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Pregame_GetMatch"):
                        if (_lastInvoke == "Pregame_GetMatch") break;
                        OnPreGameMatchLoaded?.Invoke(MiscLogic.ExtractValue(line, @"matches/([a-fA-F\d-]+)", 1));
                        _lastInvoke = "Pregame_GetMatch";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Pregame_LockCharacter"):
                        agentId = MiscLogic.ExtractValue(line, @"lock/([a-fA-F\d-]+)", 1);
                        OnAgentLocked?.Invoke(ValorantLogic.AgentIdToAgent[agentId]);
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("Pregame_SelectCharacter"):
                        if (_lastInvoke == "Pregame_SelectCharacter") break;
                        agentId = MiscLogic.ExtractValue(line, @"select/([a-fA-F\d-]+)", 1);
                        OnSelectedCharacter?.Invoke(ValorantLogic.AgentIdToAgent[agentId]);
                        _lastInvoke = "Pregame_SelectCharacter";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("LogMapLoadModel: Update: [Map Name: ") && line.Contains("192."):
                        if (_lastInvoke == "MapLoad") break;
                        OnGameLoaded?.Invoke(GetMapName(line));
                        _lastInvoke = "MapLoad";
                        _lastIndex = lineIndex;
                        break;
                    case var _ when line.Contains("LogShooterGameState: Match Ended: Completion State:"):
                        if (_lastInvoke == "MapEnd") break;
                        OnGameEnded?.Invoke(GetWinningTeam(line));
                        _lastInvoke = "MapEnd";
                        _lastIndex = lineIndex;
                        break;
                }
            }
        }

        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private void MonitorLogFile()
        {
            long lastFileSize = 0;
            Task.Run(async() =>
            {
                while (true)
                {
                    await Task.Delay(100);
                    long currentFileSize = new FileInfo(LogPath).Length;
                    if (currentFileSize == lastFileSize) continue;
                    lastFileSize = currentFileSize;
                    ParseLogText(GetLogText());
                }
            });
        }
    }
}
