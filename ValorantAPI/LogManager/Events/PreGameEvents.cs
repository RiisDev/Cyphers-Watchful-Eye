using System.Diagnostics;
using CyphersWatchfulEye.InternalLogic;

namespace CyphersWatchfulEye.ValorantAPI.LogManager.Events
{
    public class PreGameEvents(ClientData client)
    {
        private string _matchId = null!;
        public delegate void QueueEvent(string id);

        public event QueueEvent? OnPreGamePlayerLoaded;
        public event QueueEvent? OnPreGameMatchLoaded;
        public event QueueEvent? OnAgentSelected;
        public event QueueEvent? OnAgentLockedIn;

        public void HandlePreGameEvents(string invoker, string logData)
        {
            string agentId;
            switch (invoker)
            {
                case "Pregame_GetPlayer":
                    OnPreGamePlayerLoaded?.Invoke(client.UserId);
                    break;
                case "Pregame_GetMatch":
                    string matchId = MiscLogic.ExtractValue(logData, @"matches/([a-fA-F\d-]+)", 1);
                    if (string.IsNullOrEmpty(matchId)) return;
                    if (matchId == _matchId) return;
                    _matchId = matchId;
                    OnPreGameMatchLoaded?.Invoke(matchId);
                    break;
                case "Pregame_LockCharacter":
                    agentId = MiscLogic.ExtractValue(logData, @"lock/([a-fA-F\d-]+)", 1);
                    Debug.WriteLine($"Character Locked: {agentId}");
                    OnAgentLockedIn?.Invoke(ValorantLogic.AgentIdToAgent[agentId]);
                    break;
                case "Pregame_SelectCharacter":
                    agentId = MiscLogic.ExtractValue(logData, @"select/([a-fA-F\d-]+)", 1);
                    Debug.WriteLine($"Character Selected: {agentId}");
                    OnAgentSelected?.Invoke(ValorantLogic.AgentIdToAgent[agentId]);
                    break;
            }
        }
    }
}