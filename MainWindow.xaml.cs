using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using CyphersWatchfulEye.InternalLogic;
using RadiantConnect;
using RadiantConnect.Network.CurrentGameEndpoints.DataTypes;
using RadiantConnect.Network.PreGameEndpoints.DataTypes;
using RadiantConnect.Network.PVPEndpoints.DataTypes;
using Player = RadiantConnect.Network.CurrentGameEndpoints.DataTypes.Player;

namespace CyphersWatchfulEye
{
    public partial class MainWindow
    {
        private readonly List<Player> _leftPlayers = new();
        private readonly List<Player> _rightPlayers = new();
        private Initiator _initiator = null!;
        internal Dictionary<int, Dictionary<string, FrameworkElement>> LeftPlayerElements = new();
        internal Dictionary<int, Dictionary<string, FrameworkElement>> RightPlayerElements = new();

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        internal Dictionary<string, Dictionary<string, string>> MapInfo = new()
        {
            {"Juliett", new Dictionary<string, string>{{"Sunset", "pack://application:,,,/Assets/Images/Sunset.png" } }},
            {"Jam", new Dictionary<string, string>{{"Lotus", "pack://application:,,,/Assets/Images/Lotus.png" } }},
            {"Pitt", new Dictionary<string, string>{{"Pearl", "pack://application:,,,/Assets/Images/Pearl.png" } }},
            {"Canyon", new Dictionary<string, string>{{"Fracture", "pack://application:,,,/Assets/Images/Fracture.png" } }},
            {"Foxtrot", new Dictionary<string, string>{{"Breeze", "pack://application:,,,/Assets/Images/Breeze.png" } }},
            {"Port", new Dictionary<string, string>{{"Icebox", "pack://application:,,,/Assets/Images/Icebox.png" } }},
            {"Ascent", new Dictionary<string, string>{{"Ascent", "pack://application:,,,/Assets/Images/Ascent.png" } }},
            {"Bonsai", new Dictionary<string, string>{{"Split", "pack://application:,,,/Assets/Images/Split.png" } }},
            {"Triad", new Dictionary<string, string>{{"Haven", "pack://application:,,,/Assets/Images/Haven.png" } }},
            {"Duality", new Dictionary<string, string>{{"Bind", "pack://application:,,,/Assets/Images/Bind.png" } }},
        };

        private void SetupDictionaries()
        {
            _leftPlayers.Clear();
            _rightPlayers.Clear();
            LeftPlayerElements.Clear();
            RightPlayerElements.Clear();
            for (int i = 0; i < 5; i++)
            {
                LeftPlayerElements.Add(i, new Dictionary<string, FrameworkElement>());
                RightPlayerElements.Add(i, new Dictionary<string, FrameworkElement>());
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (UIElement? element in WpfWindowHelper.GetAllControls(Application.Current.MainWindow!))
                {
                    if (element is not FrameworkElement frameworkElement) continue;
                    string name = frameworkElement.Name;

                    if (frameworkElement.Name.Contains("Player") && frameworkElement.Parent is Canvas { Parent: Border border }) 
                        border.Visibility = Visibility.Hidden;

                    if (name.Contains("LeftPlayer"))
                        AddToDictionary(name, frameworkElement, LeftPlayerElements, "LeftPlayer");
                    else if (name.Contains("RightPlayer"))
                        AddToDictionary(name, frameworkElement, RightPlayerElements, "RightPlayer");
                }

                return;

                int ExtractPlayerNumber(string elementName, string playerPrefix) =>
                    int.Parse(elementName.Substring(playerPrefix.Length, 1)) - 1;

                void AddToDictionary(string elementName, FrameworkElement element, IReadOnlyDictionary<int, Dictionary<string, FrameworkElement>> playerElements, string playerPrefix) =>
                    playerElements[ExtractPlayerNumber(elementName, playerPrefix)].Add(elementName, element);
            });
            
        }

        private static void SetElementProperty(FrameworkElement element, DependencyProperty property, object newProperty)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (property == WidthProperty)
                {
                    element.Width = (double)newProperty;
                    return;
                }

                element.SetValue(property, newProperty);
            });
        }

        private static void SetPlayerControlItem(IReadOnlyDictionary<string, FrameworkElement> playerControls, string controlName, DependencyProperty property, object newProperty)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (property == Image.SourceProperty) newProperty = new BitmapImage(new Uri(newProperty.ToString()!, UriKind.Absolute));

                if (controlName.Contains("PlayerName"))
                    ((playerControls[controlName].Parent as Canvas)!.Parent as Border)!.Visibility = Visibility.Visible;

                if (property == WidthProperty)
                {
                    playerControls[controlName].Width = double.Parse($"{newProperty}.0");
                    playerControls[controlName].SetValue(BackgroundProperty, new SolidColorBrush(InternalValorantLogic.PercentToColour[int.Parse(newProperty.ToString()!)]));
                }
                else if (controlName.Contains("GameRankRating"))
                {
                    ((playerControls[controlName] as Border)!.Child as Label)!.Content = newProperty;
                    if (newProperty.ToString() == "0") playerControls[controlName].SetValue(BackgroundProperty, Brushes.LightGray);
                    else if (newProperty.ToString()![0] == '-') playerControls[controlName].SetValue(BackgroundProperty, Brushes.Red);
                    else playerControls[controlName].SetValue(BackgroundProperty, Brushes.YellowGreen);
                }
                else
                    playerControls[controlName].SetValue(property, newProperty);
            });
        }
        
        private static void SetBorderBackgroundImageSource(IReadOnlyDictionary<string, FrameworkElement>? playerControls, string? controlName, string newImageSource, Border? actual = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (actual is not null)
                {
                    if (actual.Background is ImageBrush imageBrush)
                    {
                        imageBrush.ImageSource = new BitmapImage(new Uri(newImageSource, UriKind.Absolute));
                    }
                }
                else if (playerControls?[controlName!] is Border { Background: ImageBrush imageBrush })
                {
                    imageBrush.ImageSource =  new BitmapImage(new Uri(newImageSource, UriKind.Absolute)) ;
                }
            });
        }

        private async Task<string?> GetInGameName(string userId)
        {
            NameService? nameServiceData = await _initiator.Endpoints.PvpEndpoints.FetchNameServiceReturn(userId);

            return $"{nameServiceData?.GameName}#{nameServiceData?.TagLine}";
        }

        private async Task<(ValorantRank, long, long, long, long)> GetLastRankRatings(string userId)
        {
            CompetitiveUpdate? history = await _initiator.Endpoints.PvpEndpoints.FetchCompetitveUpdatesAsync(userId);
            ValorantRank rank = ValorantRank.Default();
            long rankRatingOne = 0;
            long rankRatingTwo = 0;
            long rankRatingThree = 0;
            long currentRating = 0;
            int matchCount = history?.Matches.Count ?? 0;

            if (matchCount == 0) return (rank, currentRating, rankRatingOne, rankRatingTwo, rankRatingThree);

            rank = InternalValorantLogic.TierToRank[history?.Matches[0].TierAfterUpdate!.Value ?? 0];

            currentRating = history?.Matches[0].RankedRatingAfterUpdate ?? 0;
            
            if (matchCount >= 1)
                rankRatingOne = history?.Matches[0].RankedRatingEarned ?? 0;
            if (matchCount >= 2)
                rankRatingTwo = history?.Matches[1].RankedRatingEarned ?? 0;
            if (matchCount >= 3)
                rankRatingThree = history?.Matches[2].RankedRatingEarned ?? 0;

            return (rank, currentRating, rankRatingOne, rankRatingTwo, rankRatingThree);
        }

        private void DoGameWatcher()
        {
            _initiator = new Initiator();

            _initiator.GameEvents.Queue.OnEnteredQueue += _ => { SetElementProperty(GameState, ContentProperty, "In Queue"); };
            _initiator.GameEvents.Queue.OnLeftQueue += _ => { SetElementProperty(GameState, ContentProperty, "Lobby"); };
            _initiator.GameEvents.Queue.OnQueueChanged += data => { SetElementProperty(MatchType, ContentProperty, data?.ToUpper()!); };

            _initiator.GameEvents.Match.OnMatchEnded += _ => { SetElementProperty(GameState, ContentProperty, "Lobby"); };
            _initiator.GameEvents.Match.OnMatchStarted += Match_OnMatchStarted;
            _initiator.GameEvents.PreGame.OnPreGameMatchLoaded += PreGame_OnPreGameLoaded;
        }

        private async Task SetPlayerControls(IReadOnlyList<Player> playerList, IReadOnlyDictionary<int, Dictionary<string, FrameworkElement>> playerElements, int indexOffset = 0)
        {
            for (int playerIndex = 0; playerIndex < playerList.Count; playerIndex++)
            {
                if (playerIndex >= 5) continue; 
                double playerIndexNew = playerIndex + indexOffset + 1.0;
                SetElementProperty(LoadingPlayers, ContentProperty, $"Loading: {Math.Round((playerIndexNew / 10) * 100)}%");
                Dictionary<string, FrameworkElement> playerElement = playerElements[playerIndex];
                Player player = playerList[playerIndex];
                string? playerName = player.PlayerIdentity.Incognito ? InternalValorantLogic.AgentIdToAgent[player.CharacterID] : await GetInGameName(player.Subject);
                (ValorantRank valorantRank, long currentRating, long rankRatingOne, long rankRatingTwo, long rankRatingThree) = await GetLastRankRatings(player.Subject);

                string prefix = (playerList[0].Subject == _leftPlayers[0].Subject) ? "LeftPlayer" : "RightPlayer";
                
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}Level", ContentProperty, player.PlayerIdentity.HideAccountLevel ? -1 : player.PlayerIdentity.AccountLevel);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}PlayerName", ContentProperty, (!player.PlayerIdentity.Incognito ? playerName : InternalValorantLogic.AgentIdToAgent[player.CharacterID])!);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}AgentName", ContentProperty, InternalValorantLogic.AgentIdToAgent[player.CharacterID.ToLower()]);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}CurrentRank", Image.SourceProperty, valorantRank.RankIcon!);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}FirstGameRankRating", ContentProperty, rankRatingOne);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}SecondGameRankRating", ContentProperty, rankRatingTwo);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}ThirdGameRankRating", ContentProperty, rankRatingThree);
                SetPlayerControlItem(playerElement, $"{prefix}{playerIndex + 1}RankRatingProgress", WidthProperty, currentRating);
                SetBorderBackgroundImageSource(playerElement, $"{prefix}{playerIndex + 1}HeadshotIcon", InternalValorantLogic.AgentIdToIcon[player.CharacterID.ToLower()]);
            }
        }

        private async void Match_OnMatchStarted(string? mapName)
        {
            SetupDictionaries();
            SetElementProperty(GameState, ContentProperty, "In-Game");
            CurrentGamePlayer? currentGamePlayer = await _initiator.Endpoints.CurrentGameEndpoints.GetCurrentGamePlayerAsync(_initiator.ExternalSystem.ClientData.UserId);
            CurrentGameMatch? currentGameMatch = await _initiator.Endpoints.CurrentGameEndpoints.GetCurrentGameMatchAsync(currentGamePlayer?.MatchId!);
            string leftTeamId = currentGameMatch?.Players[0].TeamID!;
            string internalMapName = (currentGameMatch?.MapID!)[(currentGameMatch.MapID.LastIndexOf('/') + 1)..];

            SetElementProperty(MapName, ContentProperty, MapInfo[internalMapName].Keys.First());

            SetElementProperty(MatchType, ContentProperty, currentGameMatch.MatchmakingData != null! ? currentGameMatch.MatchmakingData.QueueID : "Custom");

            SetElementProperty(ServerRegion, ContentProperty, InternalValorantLogic.GamePodsDictionary[currentGameMatch.GamePodID]);
            SetBorderBackgroundImageSource(null, null, MapInfo[internalMapName].Values.First(), MapIcon);
        
            _leftPlayers.AddRange(currentGameMatch.Players.Where(player => player.TeamID == leftTeamId));
            _rightPlayers.AddRange(currentGameMatch.Players.Where(player => player.TeamID != leftTeamId));

            await SetPlayerControls(_leftPlayers, LeftPlayerElements);
            await SetPlayerControls(_rightPlayers, RightPlayerElements, 5);
            SetElementProperty(LoadingPlayers, ContentProperty, "Loading: 100%");
        }

        private async void PreGame_OnPreGameLoaded(string? matchId)
        {
            if (matchId == null) return;

            try
            {
                PreGameMatch? preGameMatch = await _initiator.Endpoints.PreGameEndpoints.FetchPreGameMatchAsync(matchId);
                SetElementProperty(MapName, ContentProperty, (preGameMatch?.MapID!)[(preGameMatch.MapID.LastIndexOf('/') + 1)..]);
                SetElementProperty(MatchType, ContentProperty, preGameMatch.QueueID.ToUpper());
                SetElementProperty(ServerRegion, ContentProperty, InternalValorantLogic.GamePodsDictionary[preGameMatch.GamePodID]);
            }
            catch (KeyNotFoundException)
            {
                PreGameMatch? preGameMatch = await _initiator.Endpoints.PreGameEndpoints.FetchPreGameMatchAsync(matchId);
                if (preGameMatch != null)
                    Debug.WriteLine($"INVALID_POD: {preGameMatch.GamePodID}");
                SetElementProperty(ServerRegion, ContentProperty, "Unknown");
            }
            catch { SetElementProperty(GameState, ContentProperty, "Lobby"); }
            finally { SetElementProperty(GameState, ContentProperty, "Agent Select"); }
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                MinWidth = Width;
                MinHeight = Height;
                MaxWidth = Width;
                MaxHeight = Height;

                SetupDictionaries();
                await Task.Run(DoGameWatcher);
            };
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) { Match_OnMatchStarted(""); }
    }
}