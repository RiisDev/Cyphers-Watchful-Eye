using CyphersWatchfulEye.InternalLogic;
using CyphersWatchfulEye.ValorantAPI.DataTypes;
using System.Text.Json;
using CyphersWatchfulEye.ValorantAPI.Methods;
using static CyphersWatchfulEye.ValorantAPI.LogManager.LogManagerEvents;

namespace CyphersWatchfulEye.ValorantAPI.LogManager.Events
{
    public class QueueEvents(ClientData client, NetHandler net)
    {
        public delegate void QueueEvent<in T>(T value);

        public event QueueEvent<CustomGameData?>? OnCustomGameLobbyCreated;
        public event QueueEvent<string?>? OnQueueChanged;
        public event QueueEvent<string?>? OnEnteredQueue;
        public event QueueEvent<string?>? OnLeftQueue;

        private string GetEndpoint(string prefix, string log) => MiscLogic.TryExtractSubstring(log, "https", ']', startIndex => startIndex != -1, prefix);

        private async Task<T?> GetPartyData<T>(PartyDataReturn dataReturn, string endPoint) where T : class?
        {
            string? data = await net.GetAsync(client.GlzUrl, endPoint);

            return data is null ? null : dataReturn switch
            {
                PartyDataReturn.CustomGame => (T)Convert.ChangeType(JsonSerializer.Deserialize<PartyInfo>(data)?.CustomGameData, typeof(T))!,
                PartyDataReturn.ChangeQueue => (T)Convert.ChangeType(JsonSerializer.Deserialize<PartyInfo>(data)?.MatchmakingData.QueueID, typeof(T))!,
                _ => throw new ArgumentOutOfRangeException(nameof(dataReturn), dataReturn, null)
            };
        }

        public async void HandleQueueEvent(string invoker, string logData)
        {
            string parsedEndPoint = logData.Replace("/queue", "")
                                    .Replace("/matchmaking/join", "")
                                    .Replace("/matchmaking/leave", "")
                                    .Replace("/makecustomgame", "");
            if (!logData.Contains("https")) return;
            
            switch (invoker)
            {
                case "Party_ChangeQueue":
                    OnQueueChanged?.Invoke(await GetPartyData<string>(PartyDataReturn.ChangeQueue, GetEndpoint(client.GlzUrl, parsedEndPoint)));
                    break;
                case "Party_EnterMatchmakingQueue":
                    OnEnteredQueue?.Invoke(await GetPartyData<string>(PartyDataReturn.ChangeQueue, GetEndpoint(client.GlzUrl, parsedEndPoint)));
                    break;
                case "Party_LeaveMatchmakingQueue":
                    OnLeftQueue?.Invoke(await GetPartyData<string>(PartyDataReturn.ChangeQueue, GetEndpoint(client.GlzUrl, parsedEndPoint)));
                    break;
                case "Party_MakePartyIntoCustomGame":
                    OnCustomGameLobbyCreated?.Invoke(await GetPartyData<CustomGameData>(PartyDataReturn.CustomGame, GetEndpoint(client.GlzUrl, parsedEndPoint)));
                    break;
            }
        }
    }
}
