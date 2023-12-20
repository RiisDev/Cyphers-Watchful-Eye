using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyphersWatchfulEye.ValorantAPI.Services;

namespace CyphersWatchfulEye.ValorantAPI.Methods
{
    public class LobbyHandler
    {
        private readonly NetHandler _net;
        public LobbyHandler(NetHandler netData) { _net = netData; }

        public enum LobbyType
        {
            Lobby,
            AgentSelect,
            Loading,
            Custom
        }

        private IReadOnlyList<UserClient> GetAgentSelectData()
        {
            // get pregame match id https://valapidocs.techchrism.me/endpoint/pre-game-player
            // Get pregame data: https://glz-{region}-1.{shard}.a.pvp.net/pregame/v1/matches/{pre-game match id}
            return new List<UserClient>();
        }

        private IReadOnlyList<UserClient> GetCustomPartyData()
        {
            // get partyid:  https://glz-{region}-1.{shard}.a.pvp.net/parties/v1/players/{puuid}
            // Get party data: https://glz-{region}-1.{shard}.a.pvp.net/parties/v1/parties/{party id}
            return new List<UserClient>();
        }

        private IReadOnlyList<UserClient> GetLoadingData()
        {
            // get current game matchid: https://glz-{region}-1.{shard}.a.pvp.net/core-game/v1/players/{puuid}
            // get match data: https://glz-{region}-1.{shard}.a.pvp.net/core-game/v1/matches/{current game match id}
            return new List<UserClient>();
        }

        public IReadOnlyList<UserClient> GetCurrentClients(LobbyType type)
        {
            return type switch
            {
                LobbyType.AgentSelect => GetAgentSelectData(),
                LobbyType.Loading => GetLoadingData(),
                LobbyType.Lobby => GetCustomPartyData(),
                LobbyType.Custom => GetCustomPartyData(),
                _ => GetCustomPartyData()
            };
        }

    }
}
