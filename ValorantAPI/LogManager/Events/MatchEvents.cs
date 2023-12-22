using CyphersWatchfulEye.InternalLogic;

namespace CyphersWatchfulEye.ValorantAPI.LogManager.Events
{
    public class MatchEvents
    {
        private string? _mapName;
        private static string GetWinningTeam(string log) => MiscLogic.TryExtractSubstring(log, "Team: ", '(', startIndex => startIndex >= 0).Trim().Replace("Team:'", "")[..^1];
        private static string GetMapName(string log) => MiscLogic.TryExtractSubstring(log, "Map Name:", '|', startIndex => startIndex >= 0, "Map Name: ").Trim();
        
        public delegate void MatchEvent<in T>(T value);

        public event MatchEvent<string?>? OnMapLoaded;
        public event MatchEvent<string>? OnMatchEnded;
        public event MatchEvent<string?>? OnMatchStarted;

        public void HandleMatchEvent(string invoker, string logData)
        {
            switch (invoker)
            {
                case "Map_Loaded":
                    _mapName = GetMapName(logData);
                    OnMapLoaded?.Invoke(GetMapName(logData));
                    break;
                case "Match_Ended":
                    OnMatchEnded?.Invoke(GetWinningTeam(logData));
                    _mapName = null;
                    break;
                case "Match_Started":
                    OnMatchStarted?.Invoke(_mapName);
                    break;
            }
        }
    }
}
