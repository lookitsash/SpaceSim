using System;
using System.Threading;
using System.Collections.Generic;

namespace SpaceSim
{
    static class Program
    {
        public static ConsoleWindow ConsoleWindow;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            (ConsoleWindow = new ConsoleWindow()).Show();
            ConsoleWindow.OnInput += new ConsoleWindow.ConsoleInputEventHandler(OnConsoleInput);

            ConsoleWindow.LogTimestamp = false;
            ConsoleWindow.Log("Welcome to my SpaceSim game in development.");
            ConsoleWindow.LogTimestamp = true;

            using (SpaceGame game = new SpaceGame())
            {
                SpaceSimLibrary.Networking.Server.StartServer();
                game.Run();
            }

            /*
            using (SpaceSimGame game = new SpaceSimGame(GameDisplayType.Cockpit))
            {
                SpaceSimLibrary.Networking.Server.StartServer();
                game.Run();
            }
             */
            /*
            List<ManualResetEvent> manualEvents = new List<ManualResetEvent>();
            manualEvents.Add(SpawnThread(GameDisplayType.Cockpit));
            manualEvents.Add(SpawnThread(GameDisplayType.LeftSide));            
            WaitHandle.WaitAll(manualEvents.ToArray());
             */
        }

        static void LaunchGame(object obj)
        {
            using (SecondaryGame secondaryGame = new SecondaryGame(GameDisplayType.LeftSide))
            {
                ((SpaceSimGame)obj).SecondaryGame = secondaryGame;
                secondaryGame.Run();
            }
        }

        static void OnConsoleInput(string str)
        {
            if (str == "exit" || str == "quit")
            {
            }
        }

        static ManualResetEvent SpawnThread(GameDisplayType gameDisplayType)
        {
            ThreadData threadData = new ThreadData() { ManualResetEvent = new ManualResetEvent(false), GameDisplayType = gameDisplayType };
            Thread thread = new Thread(new ParameterizedThreadStart(SpawnGame));
            thread.IsBackground = false;
            thread.Start(threadData);
            return threadData.ManualResetEvent;
        }

        static void SpawnGame(object data)
        {
            ThreadData threadData = (ThreadData)data;
            try
            {
                using (SpaceSimGame game = new SpaceSimGame((GameDisplayType)threadData.GameDisplayType))
                {
                    game.Run();
                }
            }
            finally
            {
                threadData.ManualResetEvent.Set();
            }
        }
    }

    public class ThreadData
    {
        public ManualResetEvent ManualResetEvent;
        public GameDisplayType GameDisplayType;
    }
}

