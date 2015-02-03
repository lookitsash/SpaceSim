using System;
using SpaceSimLibrary.Networking;
using System.Threading;
using Microsoft.Xna.Framework;

namespace SpaceSimV2
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            //Client client = new Client();
            //new Thread(new ThreadStart(StartThread)).Start();

            /*
            CommandWriter CommandWriter = new SpaceSimLibrary.Networking.CommandWriter();
            CommandWriter.WriteCommand(Commands.UpdateEntity);
            CommandWriter.WriteData(4251);
            CommandWriter.WriteMatrix(Matrix.Identity);
            Server.Broadcast(CommandWriter.GetBytes());
            CommandWriter.Reset();
             */

            /*
            Client client = new Client();
            CommandWriter cw = new CommandWriter(Commands.RegisterEntity);
            cw.WriteData(1);
            cw.WriteData((byte)3);
            cw.WriteMatrix(Matrix.Identity);
            Server.Broadcast(cw.GetBytes());
            new Thread(new ThreadStart(StartThread)).Start();
            */

            using (SpaceSimGame game = new SpaceSimGame())
            {
                game.Run();
            }
        }

        static void StartThread()
        {
            Thread.Sleep(1000);
            int i = 0;
            while (true)
            {
                //Server.Broadcast(Server.BuildCommand(Commands.RegisterEntity, (int)1, (byte)3, 1.1f, 2.2f, 3.3f));
                i++;
                if (i > 10)
                {
                    Thread.Sleep(500);
                    i = 0;
                }
            }
        }
    }
#endif
}

