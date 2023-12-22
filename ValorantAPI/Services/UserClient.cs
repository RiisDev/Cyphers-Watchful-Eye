using System.Diagnostics;
using System.Text.Json;
using CyphersWatchfulEye.InternalLogic;
using CyphersWatchfulEye.ValorantAPI.DataTypes;
using CyphersWatchfulEye.ValorantAPI.Methods;

namespace CyphersWatchfulEye.ValorantAPI.Services
{
    public class UserClient(ValorantClient config, string userId, LogManager.LogManager logStats, NetHandler net)
    {
        internal readonly NetHandler Net = net;

        public readonly LogManager.LogManager LogStats = logStats;
        public ValorantClient ValorantClient { get; set; } = config;

        public string UserId { get; set; } = userId;

        public string SeasonId { get; set; } = null!;
        public string? TrackerUrl { get; set; }
        public List<ValorantRank> Ranks { get; set; } = null!;
        public List<int> RankRating { get; set; } = null!;

        internal async Task<Dictionary<string, MatchDetails?>?> ParseMatches(IReadOnlyList<MatchData>? matches)
        {
            Dictionary<string, MatchDetails?> matchDetail = new();

            for (int i = 0; i < matches?.Count; i++)
            {
                try
                {
                    MatchData match = matches[i];
                    string? matchDetails = await Net.GetAsync(LogStats.ClientData.PdUrl, $"/match-details/v1/matches/{match.MatchID}");
                    if (string.IsNullOrEmpty(matchDetails)) continue;
                    if (matchDetails.Contains("MATCH_NOT_FOUND")) continue;

                    matchDetail.Add(match.MatchID, JsonSerializer.Deserialize<MatchDetails>(matchDetails));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            return matchDetail;
        }
        
        // if (stringCurrentRank.Contains("Unranked")) stringCurrentRank = stringCurrentRank[..7];
        public async Task<(ValorantRank, ValorantRank, ValorantRank, ValorantRank, int, int, int)> GetRankingStatsAsync(string queueType = "competitive")
        {
            // I'll name these better later!
            ValorantRank currentRank = ValorantRank.Default();
            ValorantRank lastRank = ValorantRank.Default();
            ValorantRank lastLastRank = ValorantRank.Default();
            ValorantRank lastLastLastRank = ValorantRank.Default();

            int? lastRating = null;
            int? lastLastRating = null;
            int? lastLastLastRating = null;

            /* End of Var Setup */
            
            string? data = await Net.GetAsync(LogStats.ClientData.PdUrl, $"/mmr/v1/players/{UserId}/competitiveupdates?startIndex=0&endIndex=20&queue={queueType}");
            if (string.IsNullOrEmpty(data) || !data.Contains('[')) goto DoReturn;

            MatchDataContainer? matchContainer = JsonSerializer.Deserialize<MatchDataContainer>(data);
            IReadOnlyList<MatchData>? matches = matchContainer?.Matches; 
            if (matches is null || matches.Count <= 0) goto DoReturn;

            string currentRankName = ((RankIndex.Ranks)(matches[0].TierAfterUpdate ?? 0)).ToString().Replace("_", " ");
            currentRank = new ValorantRank(currentRankName, RankIcons.RankIcon[currentRankName]);
            lastRating = matches[0].RankedRatingEarned ?? 0;

            if (matches.Count >= 2)
                lastLastRating = matches[1].RankedRatingEarned ?? 0;
            if (matches.Count >= 3)
                lastLastLastRating = matches[2].RankedRatingEarned ?? 0;

            data = await Net.GetAsync(LogStats.ClientData.PdUrl, $"/mmr/v1/players/{UserId}");
            if (string.IsNullOrEmpty(data) || !data.Contains('[')) goto DoReturn;

            Debug.WriteLine(data);

            DoReturn:
            return (
                currentRank,
                lastRank,
                lastLastRank,
                lastLastLastRank,
                lastRating ?? 0,
                lastLastRating ?? 0,
                lastLastLastRating ?? 0
            );
        }

        public async Task SetSeasonId()
        {
            string? data = await Net.GetAsync(LogStats.ClientData.SharedUrl, "/content-service/v3/content");

            if (string.IsNullOrEmpty(data)) return;

            Content? seasonContent = JsonSerializer.Deserialize<Content>(data);

            Season? season = seasonContent?.Seasons.FirstOrDefault(s => s.IsActive && s.Name.Contains("ACT"));

            SeasonId = season?.SeasonId ?? "";
        }

        public async Task GetStats()
        {
            if (string.IsNullOrEmpty(SeasonId)) await SetSeasonId();
            (ValorantRank rank, ValorantRank rank1, ValorantRank rank2, ValorantRank rank3, int rankRating1, int rankRating2, int rankRating3) = await GetRankingStatsAsync();

            Ranks.AddRange(new[]{rank,rank1,rank2,rank3});
            RankRating.AddRange(new[]{rankRating1,rankRating2,rankRating3});
        }
    }
}
