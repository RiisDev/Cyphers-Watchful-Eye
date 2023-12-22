using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using CyphersWatchfulEye.InternalLogic;
using CyphersWatchfulEye.ValorantAPI.LogManager;
using CyphersWatchfulEye.ValorantAPI.Methods;
using CyphersWatchfulEye.ValorantAPI.Services;

namespace CyphersWatchfulEye
{
    public partial class MainWindow
    {
        internal Dictionary<int, Dictionary<string, FrameworkElement>> LeftPlayerElements = new();
        internal Dictionary<int, Dictionary<string, FrameworkElement>> RightPlayerElements = new();

        private void SetupDictionaries()
        {
            for (int i = 0; i < 5; i++)
            {
                LeftPlayerElements.Add(i, new Dictionary<string, FrameworkElement>());
                RightPlayerElements.Add(i, new Dictionary<string, FrameworkElement>());
            }

            foreach (UIElement? element in WpfWindowHelper.GetAllControls(Application.Current.MainWindow!))
            {
                if (element is not FrameworkElement frameworkElement) continue;

                string name = frameworkElement.Name;
                if (name.Contains("LeftPlayer"))
                    AddToDictionary(name, frameworkElement, LeftPlayerElements, "LeftPlayer");
                else if (name.Contains("RightPlayer"))
                    AddToDictionary(name, frameworkElement, RightPlayerElements, "RightPlayer");
            }

            return;

            int ExtractPlayerNumber(string elementName, string playerPrefix) =>
                int.Parse(elementName.Substring(playerPrefix.Length, 1)) - 1;

            void AddToDictionary(string elementName, FrameworkElement element, Dictionary<int, Dictionary<string, FrameworkElement>> playerElements, string playerPrefix) =>
                playerElements[ExtractPlayerNumber(elementName, playerPrefix)].Add(elementName, element);
        }

        private void SetPlayerControlItem(IReadOnlyDictionary<string, FrameworkElement> playerControls, string controlName, DependencyProperty property, object newProperty)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                playerControls[controlName].SetValue(property, newProperty);
            });
        }

        private static async Task ValorantHandler()
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Valorant", "Saved", "Logs", "ShooterGame.log");
            do await Task.Delay(50);
            while (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length == 0);
            do await Task.Delay(50);
            while (!File.Exists(logPath));

            ValorantClient valorantClient = new(logPath);
            NetHandler net = new(valorantClient);
            LogManager valorantManager = new(net);

            valorantManager.Events.Queue.OnQueueChanged += (data) =>
            {
                Debug.WriteLine($"OnQueueChanged: {data}");
            };
            valorantManager.Events.Queue.OnEnteredQueue += (data) =>
            {
                Debug.WriteLine($"OnEnteredQueue: {data}");
            };
            valorantManager.Events.Queue.OnCustomGameLobbyCreated += (data) =>
            {
                Debug.WriteLine($"OnCustomGameLobbyCreated: {JsonSerializer.Serialize(data)}");
            };
            valorantManager.Events.Queue.OnLeftQueue += (data) =>
            {
                Debug.WriteLine($"OnLeftQueue: {data}");
            };

            valorantManager.Events.PreGame.OnAgentLockedIn += (data) =>
            {
                Debug.WriteLine($"OnAgentLockedIn: {data}");
            };
            valorantManager.Events.PreGame.OnPreGameMatchLoaded += (data) =>
            {
                Debug.WriteLine($"OnPreGameMatchLoaded: {data}");
            };
            valorantManager.Events.PreGame.OnPreGamePlayerLoaded += (data) =>
            {
                Debug.WriteLine($"OnPreGamePlayerLoaded: {data}");
            };
            valorantManager.Events.PreGame.OnAgentSelected += (data) =>
            {
                Debug.WriteLine($"OnAgentSelected: {data}");
            };

            valorantManager.Events.Match.OnMapLoaded += (data) =>
            {
                Debug.WriteLine($"OnMapLoaded: {data}");
            };
            valorantManager.Events.Match.OnMatchEnded += (data) =>
            {
                Debug.WriteLine($"OnMatchEnded: {data}");
            };
            valorantManager.Events.Match.OnMatchStarted += (data) =>
            {
                Debug.WriteLine($"OnMatchStarted: {data}");
            };

            valorantManager.Events.Round.OnRoundStarted += (data) =>
            {
                Debug.WriteLine($"OnRoundStarted: {data}");
            };
            valorantManager.Events.Round.OnRoundEnded += (data) =>
            {
                Debug.WriteLine($"OnMatchEnded: {data}");
            };

            valorantManager.Events.Vote.OnVoteDeclared += (data) =>
            {
                Debug.WriteLine($"Vote Active: {data}");
            };
            valorantManager.Events.Vote.OnVoteInvoked += (data) =>
            {
                Debug.WriteLine($"Chose Option: {data}");
            };

            valorantManager.Events.InGame.OnUtilPlaced += (data) =>
            {
                Debug.WriteLine($"OnUtilPlaced: {data}");
            };
            valorantManager.Events.InGame.OnBuyMenuClosed += (data) =>
            {
                Debug.WriteLine($"OnBuyMenuClosed: {data}");
            };
            valorantManager.Events.InGame.OnBuyMenuOpened += (data) =>
            {
                Debug.WriteLine($"OnBuyMenuOpened: {data}");
            };
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

                SetPlayerControlItem(LeftPlayerElements[0], "LeftPlayer1AgentName", ContentProperty, "test");
                await Task.Run(ValorantHandler);
            };
        }
    }
}