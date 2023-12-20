using System.Diagnostics;
using System.IO;
using System.Windows;
using CyphersWatchfulEye.InternalLogic;
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

            valorantManager.OnQueueChanged += (data) => {
                Debug.WriteLine($"OnQueueChanged: {data}");
            };
            valorantManager.OnEnteredQueue += (data) => {
                Debug.WriteLine($"OnEnteredQueue: {data}");
            };
            valorantManager.OnLeftQueue += (data) => {
                Debug.WriteLine($"OnLeftQueue: {data}");
            };
            valorantManager.OnPreGamePlayerLoaded += (data) => {
                Debug.WriteLine($"OnPreGamePlayerLoaded: {data}");
            };
            valorantManager.OnPreGameMatchLoaded += (data) => {
                Debug.WriteLine($"OnPreGameMatchLoaded: {data}");
            };
            valorantManager.OnAgentLocked += (data) => {
                Debug.WriteLine($"OnAgentLocked: {data}");
            };
            valorantManager.OnSelectedCharacter += (data) => {
                Debug.WriteLine($"OnSelectedCharacter: {data}");
            };
            valorantManager.OnGameLoaded += (data) => {
                Debug.WriteLine($"OnGameLoaded: {data}");
            };
            valorantManager.OnGameEnded += (data) => {
                Debug.WriteLine($"OnGameEnded: {data}");
            };
            valorantManager.OnCustomGameLobbyCreated += (data) => {
                Debug.WriteLine($"OnCustomGameLobbyCreated: {data}");
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