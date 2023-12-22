using CyphersWatchfulEye.InternalLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyphersWatchfulEye.ValorantAPI.LogManager.Events
{
    public class InGameEvents
    {
        public delegate void InGameEvent<in T>(T value);

        public event InGameEvent<int?>? OnBuyMenuOpened;
        public event InGameEvent<int?>? OnBuyMenuClosed;

        public event InGameEvent<string?>? OnUtilPlaced; 


        public void HandleInGameEvent(string invoker, string logData)
        {
            switch (invoker)
            {
                case "Buy_Menu_Opened":
                    OnBuyMenuOpened?.Invoke(1);
                    break;
                case "Buy_Menu_Closed":
                    OnBuyMenuClosed?.Invoke(0);
                    break;
                case "Util_Placed":
                    string util = MiscLogic.ExtractValue(logData, @"actor\s(\S+)(?=\.)", 1);
                    OnUtilPlaced?.Invoke(util);
                    break;
            }
        }
    }
}
