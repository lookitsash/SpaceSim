using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace SpaceSim
{
    public class CustomGraphicsDeviceManager : GraphicsDeviceManager 
    {
        public GameDisplayType GameDisplayType;

        public int DisplayID
        {
            get
            {
                if (GameDisplayType == SpaceSim.GameDisplayType.LeftSide) return 4;
                else if (GameDisplayType == SpaceSim.GameDisplayType.RightSide) return 1;
                return 3;
            }
        }

        public CustomGraphicsDeviceManager(Game game, GameDisplayType gameDisplayType)
            : base(game)
        {
            GameDisplayType = gameDisplayType;
            this.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(CustomGraphicsDeviceManager_PreparingDeviceSettings);
        }

        protected void CustomGraphicsDeviceManager_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.IsFullScreen = false;
        }

        protected override void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            List<GraphicsDeviceInformation> removals = new List<GraphicsDeviceInformation>();
            for (int i = 0; i < foundDevices.Count; i++)
            {
                GraphicsDeviceInformation gdi = foundDevices[i];
                if (!gdi.Adapter.DeviceName.Contains("DISPLAY" + DisplayID))
                    removals.Add(foundDevices[i]);
            }
            foreach (GraphicsDeviceInformation info in removals)
            {
                foundDevices.Remove(info);
            }
            base.RankDevices(foundDevices);
        }
    }

    public enum GameDisplayType
    {
        Cockpit,
        LeftSide,
        RightSide
    }
}
