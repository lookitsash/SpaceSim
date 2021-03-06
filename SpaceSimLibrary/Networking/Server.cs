﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using BEPUphysics.Entities.Prefabs;

namespace SpaceSimLibrary.Networking
{
    public class ServerEntity
    {
        public int ID, SyncInterval = 250;
        public PhysicsShape PhysicsShape;
        public EntityType EntityType;
        public Matrix World;
        public Vector3 Scale, AngularVelocity, LinearVelocity;
        public float AngularDamping, LinearDamping, Radius;
        public bool Updated;
        
        private DateTime SyncDate = DateTime.Now;

        public bool ShouldSync
        {
            get
            {
                return (DateTime.Now - SyncDate).TotalMilliseconds >= SyncInterval;
            }
        }

        public void ResetSync() { SyncDate = DateTime.Now; }
    }

    public class Server
    {
        //static byte[] BroadcastBuffer = new byte[32768]; // Large udp packets results in many losses over network
        static byte[] BroadcastBuffer = new byte[1024];
        static bool Active = false;
        static Dictionary<int, ServerEntity> ServerEntities = new Dictionary<int, ServerEntity>();
        static List<int> ServerEntityIDs = new List<int>();

        public static void UpdateServerEntity(GameEntity gameEntity)
        {
            ServerEntity entity = ServerEntities.ContainsKey(gameEntity.ID) ? ServerEntities[gameEntity.ID] : null;
            if (entity == null)
            {
                ServerEntities.Add(gameEntity.ID, entity = new ServerEntity() { ID = gameEntity.ID, EntityType = gameEntity.EntityType, Radius = ((Sphere)gameEntity.PhysicsEntity).Radius });
                if (gameEntity.EntityType == EntityType.Player) entity.SyncInterval = 10;
                ServerEntityIDs.Add(gameEntity.ID);
            }
            entity.World = gameEntity.World;
            entity.AngularDamping = gameEntity.PhysicsEntity.AngularDamping;
            entity.LinearDamping = gameEntity.PhysicsEntity.LinearDamping;
            entity.Scale.X = gameEntity.Scale.M11;
            entity.Scale.Y = gameEntity.Scale.M22;
            entity.Scale.Z = gameEntity.Scale.M33;            
            entity.AngularVelocity.X = gameEntity.PhysicsEntity.AngularVelocity.X;
            entity.AngularVelocity.Y = gameEntity.PhysicsEntity.AngularVelocity.Y;
            entity.AngularVelocity.Z = gameEntity.PhysicsEntity.AngularVelocity.Z;
            entity.LinearVelocity.X = gameEntity.PhysicsEntity.LinearVelocity.X;
            entity.LinearVelocity.Y = gameEntity.PhysicsEntity.LinearVelocity.Y;
            entity.LinearVelocity.Z = gameEntity.PhysicsEntity.LinearVelocity.Z;            
            entity.Updated = true;
        }

        private static void SyncServerEntities()
        {
            CommandWriter cw = new CommandWriter();
            while (Active)
            {
                bool sendPending = false;

                int entityCount = ServerEntityIDs.Count;
                for (int i = 0; i < entityCount; i++)
                {
                    ServerEntity entity = ServerEntities[ServerEntityIDs[i]];
                    if (entity.Updated && entity.ShouldSync)
                    {
                        if (entity.ID == 2)
                        {
                        }
                        cw.Reset();
                        cw.WriteCommand(Commands.UpdateEntity);
                        cw.WriteData(entity.ID);
                        cw.WriteData((byte)entity.EntityType);
                        cw.WriteMatrix(entity.World);
                        cw.WriteVector3(entity.Scale);
                        cw.WriteData((byte)entity.PhysicsShape);
                        cw.WriteData(entity.Radius);
                        cw.WriteData(entity.AngularDamping);
                        cw.WriteData(entity.LinearDamping);                        
                        cw.WriteVector3(entity.AngularVelocity);
                        cw.WriteVector3(entity.LinearVelocity);
                        if (FillBuffer(cw.GetBytes())) sendPending = true;
                        else
                        {
                            FlushBuffer();
                            FillBuffer(cw.GetBytes());
                            sendPending = false;
                        }
                        entity.Updated = false;
                        entity.ResetSync();
                    }
                }
                if (sendPending) FlushBuffer();
                Thread.Sleep(1);
            }
        }

        private static bool FillBuffer(byte[] data)
        {
            if ((BroadcastBufferWritePos + data.Length) > BroadcastBuffer.Length) return false;

            Buffer.BlockCopy(data, 0, BroadcastBuffer, BroadcastBufferWritePos, data.Length);
            BroadcastBufferWritePos += data.Length;
            
            return true;
        }

        private static void FlushBuffer()
        {
            UdpClient.Client.BeginSendTo(BroadcastBuffer, 0, BroadcastBufferWritePos, SocketFlags.None, MulticastEndpoint, _AsyncCallback, _Socket);
            BroadcastBufferWritePos = 0;
        }

        static Server()
        {
            //BW = new BinaryWriter(new MemoryStream());
            
        }

        private static UdpClient UdpClient;
        private static IPEndPoint MulticastEndpoint;
        private static IBroadcaster Broadcaster;

        public static void StartServer()
        {
            UdpClient = new UdpClient();
            IPAddress multicastaddress = IPAddress.Parse("239.0.0.222");
            UdpClient.JoinMulticastGroup(multicastaddress);
            MulticastEndpoint = new IPEndPoint(multicastaddress, 2222);

            Active = true;
            new Thread(new ThreadStart(SyncServerEntities)).Start();
            //new Thread(new ThreadStart(ProcessQueue)).Start();
            //new Thread(new ThreadStart(ProcessBroadcasterQueue)).Start();
        }

        public static void StopServer()
        {
            Active = false;
        }

        private static void ProcessBroadcasterQueue()
        {
            while (true)
            {
                Broadcaster.ProcessBroadcastQueue();
                Thread.Sleep(1);
            }
        }

        private static DateTime LastDisplay = DateTime.Now;
        private static void ProcessQueue()
        {
            while (true)
            {
                Broadcaster.Broadcast();
                lock (BroadcastBufferLock)
                {
                    if ((DateTime.Now - LastDisplay).TotalSeconds > 1)
                    {
                        //Console.WriteLine("BroadcastBufferReadPos: " + BroadcastBufferReadPos + ", BroadcastBufferWritePos: " + BroadcastBufferWritePos);
                        LastDisplay = DateTime.Now;
                    }
                    if (BroadcastBufferReadPos != BroadcastBufferWritePos)
                    {
                        //Console.WriteLine("BroadcastBufferReadPos: " + BroadcastBufferReadPos + ", BroadcastBufferWritePos: " + BroadcastBufferWritePos);
                        _BroadcastFunc(BroadcastBuffer, BroadcastBufferReadPos, BroadcastBufferWritePos - BroadcastBufferReadPos);
                        BroadcastBufferReadPos = BroadcastBufferWritePos;
                    }
                }
                Thread.Sleep(1);
                /*
                int queueCount = Queue.Count;
                if (queueCount > 0)
                {
                    if ((DateTime.Now - LastDisplay).TotalSeconds > 1)
                    {
                        Console.WriteLine("QueueSize: " + queueCount);
                        LastDisplay = DateTime.Now;
                    }

                    byte[] data = Queue.Dequeue();
                    if (data != null) _BroadcastFunc(data);
                }
                */
            }
        }

        private static Queue<byte[]> Queue = new Queue<byte[]>(999999);

        private static Socket _Socket = null;
        private static IPAddress _Broadcast = null;
        private static IPEndPoint _Endpoint = null;
        private static AsyncCallback _AsyncCallback;

        private static IPAddress _Broadcast2 = null;
        private static IPEndPoint _Endpoint2 = null;

        //private static BinaryWriter BW;
        private static object BroadcastBufferLock = new object();
        private static int BroadcastBufferWritePos = 0;
        private static int BroadcastBufferReadPos = 0;

        public static bool Broadcast(byte[] data)
        {
            lock (BroadcastBufferLock)
            {
                if ((BroadcastBufferWritePos + data.Length) > BroadcastBuffer.Length)
                {
                    // Shift buffer
                    byte[] newBroadcastBuffer = new byte[BroadcastBuffer.Length];
                    for (int i = BroadcastBufferReadPos, j = 0; i < BroadcastBufferWritePos; i++, j++)
                    {
                        newBroadcastBuffer[j] = BroadcastBuffer[i];
                    }
                    BroadcastBuffer = newBroadcastBuffer;
                    BroadcastBufferWritePos -= BroadcastBufferReadPos;
                    BroadcastBufferReadPos = 0;

                    if ((BroadcastBufferWritePos + data.Length) > BroadcastBuffer.Length)
                    {
                        //Console.WriteLine("IGNORING BROADCAST");
                        return false;
                    }
                }

                for (int i = 0; i < data.Length; i++)
                {
                    BroadcastBuffer[BroadcastBufferWritePos++] = data[i];
                }
                return true;
            }
        }

        private static void _BroadcastFunc(byte[] data)
        {
            _BroadcastFunc(data, 0, data.Length);
        }

        private static void _BroadcastFunc(byte[] data, int offset, int size)
        {
            
            /*
            if (_Socket == null) _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (_Broadcast == null) _Broadcast = IPAddress.Parse("192.168.1.255");
            if (_Endpoint == null) _Endpoint = new IPEndPoint(_Broadcast, 12345);
            if (_Broadcast2 == null) _Broadcast2 = IPAddress.Parse("192.168.1.255");
            if (_Endpoint2 == null) _Endpoint2 = new IPEndPoint(_Broadcast, 12346);
            if (_AsyncCallback == null) _AsyncCallback = new AsyncCallback(SendCallback);

            //byte[] sendbuf = Encoding.ASCII.GetBytes(data);
            //_Socket.SendTo(data, 0, data.Length, SocketFlags.None, _Endpoint);
            _Socket.BeginSendTo(data, offset, size, SocketFlags.None, _Endpoint, _AsyncCallback, _Socket);
            _Socket.BeginSendTo(data, offset, size, SocketFlags.None, _Endpoint2, _AsyncCallback, _Socket);
             */

            UdpClient.Client.BeginSendTo(data, offset, size, SocketFlags.None, MulticastEndpoint, _AsyncCallback, _Socket);
        }

        public static byte[] BuildCommand(Commands cmd, params object [] parameters)
        {
            List<byte> bytes = new List<byte>();

            bytes.Add(Convert.ToByte((int)cmd));
            for (int i = 0; i < parameters.Length; i++)
            {
                object obj = parameters[i];
                if (obj is byte) bytes.Add((byte)obj);
                else
                {
                    byte[] objBytes = null;
                    if (obj is int) objBytes = BitConverter.GetBytes((int)obj);
                    else if (obj is float) objBytes = BitConverter.GetBytes((float)obj);
                    else if (obj is string) objBytes = Encoding.UTF8.GetBytes((string)obj);
                    if (objBytes != null)
                    {
                        for (int j = 0; j < objBytes.Length; j++) bytes.Add(objBytes[j]);
                    }
                }
            }
            
            return bytes.ToArray();
        }

        private static void SendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = client.EndSend(ar);
        }
    }

    public enum Commands
    {
        RegisterEntity = 1,
        RemoveEntity,
        UpdateEntity
    }
}
