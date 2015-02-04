using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.Xna.Framework;

namespace SpaceSimLibrary.Networking
{
    public delegate void CommandReceived(CommandReader cr);

    public class Client
    {
        private UdpClient UdpClient;
        public Client(int port)
        {
            //UdpClient = new UdpClient(port);
            //UdpClient.BeginReceive(new AsyncCallback(dataReceived), null);
            UdpClient = new UdpClient();
            UdpClient.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 2222);

            UdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            UdpClient.ExclusiveAddressUse = false;

            UdpClient.Client.Bind(localEp);

            IPAddress multicastaddress = IPAddress.Parse("239.0.0.222");
            UdpClient.JoinMulticastGroup(multicastaddress);
            UdpClient.BeginReceive(new AsyncCallback(dataReceived), null);
        }

        public event CommandReceived OnCommandReceived;

        private void dataReceived(IAsyncResult res)
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = UdpClient.EndReceive(res, ref remoteIpEndPoint);

            if (data.Length > 0)
            {
                int packetPos = 0;
                while (packetPos < data.Length)
                {
                    byte packetSize = data[packetPos++];
                    if (packetSize < data.Length)
                    {
                        byte[] packetData = new byte[packetSize];
                        for (int i = packetPos, j = 0; i < packetPos + packetSize; i++, j++) packetData[j] = data[i];
                        CommandReader cr = new CommandReader(packetData);
                        if (OnCommandReceived != null) OnCommandReceived(cr);

                        /*
                        Commands cmd = cr.ReadCommand();
                        if (cmd == Commands.UpdateEntity)
                        {
                            int entityID = cr.ReadData<int>();
                            Matrix matrix = cr.ReadMatrix();
                        }
                        //Console.WriteLine("RCVD broadcast: " + packetData.Length + " bytes, " + cmd.ToString());
                         */
                        packetPos += packetSize;
                    }
                }
            }

            /*
            CommandReader cr = new CommandReader(data);

            Commands cmd = cr.ReadCommand();

            if (cmd == Commands.RegisterEntity)
            {
                //int entityID = cr.ReadData<int>();
                //byte entityType = cr.ReadData<byte>();
                //Matrix matrix = cr.ReadMatrix();
            }
            */

            //Console.WriteLine("RCVD broadcast: " + data.Length + " bytes"); //, " + cmd.ToString());
            //Console.WriteLine("Received broadcast from {0} :\n {1}\n", remoteIpEndPoint.ToString(), Encoding.ASCII.GetString(data, 0, data.Length));

            UdpClient.BeginReceive(new AsyncCallback(dataReceived), null);
        }


    }

    public class CommandWriter
    {
        private List<byte> Bytes = new List<byte>();

        public CommandWriter()
        {
            Reset();
        }

        public CommandWriter(Commands cmd)
        {
            WriteCommand(cmd);
        }

        public void Reset()
        {
            Bytes.Clear();
            Bytes.Add(0);
        }

        public void WriteCommand(Commands cmd)
        {
            WriteData((byte)cmd);
        }

        public void WriteMatrix(Matrix matrix)
        {
            WriteData(matrix.M11);
            WriteData(matrix.M12);
            WriteData(matrix.M13);
            WriteData(matrix.M14);
            WriteData(matrix.M21);
            WriteData(matrix.M22);
            WriteData(matrix.M23);
            WriteData(matrix.M24);
            WriteData(matrix.M31);
            WriteData(matrix.M32);
            WriteData(matrix.M33);
            WriteData(matrix.M34);
            WriteData(matrix.M41);
            WriteData(matrix.M42);
            WriteData(matrix.M43);
            WriteData(matrix.M44);
        }

        public void WriteData(object obj)
        {
            if (obj is byte) Bytes.Add((byte)obj);
            else
            {
                byte[] objBytes = null;
                if (obj is int) objBytes = BitConverter.GetBytes((int)obj);
                else if (obj is float) objBytes = BitConverter.GetBytes((float)obj);
                else if (obj is string) objBytes = Encoding.UTF8.GetBytes((string)obj);
                if (objBytes != null)
                {
                    for (int j = 0; j < objBytes.Length; j++) Bytes.Add(objBytes[j]);
                }
            }
        }

        public byte[] GetBytes()
        {
            Bytes[0] = (byte)(Bytes.Count - 1);
            return Bytes.ToArray();
        }
    }

    public class CommandReader
    {
        private byte[] Data = null;
        private int DataPos = 0;

        public CommandReader(byte[] data)
        {
            Data = data;
        }

        public bool HasMore { get { return DataPos < Data.Length; } }

        public Commands ReadCommand()
        {
            return (Commands)ReadData<byte>();
        }

        public T ReadData<T>()
        {
            if (typeof(T) == typeof(byte))
            {
                T value = (T)(object)Data[DataPos];
                DataPos += sizeof(byte);
                return value;
            }
            else if (typeof(T) == typeof(int))
            {
                T value = (T)(object)BitConverter.ToInt32(Data, DataPos);
                DataPos += sizeof(int);
                return value;
            }
            else if (typeof(T) == typeof(float))
            {
                T value = (T)(object)BitConverter.ToSingle(Data, DataPos);
                DataPos += sizeof(float);
                return value;
            }
            return default(T);
        }

        public Matrix ReadMatrix()
        {
            return new Matrix(ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>(), ReadData<float>());
        }
    }
}
