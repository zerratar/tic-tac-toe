using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class GameServer : IDisposable
{
    private readonly TcpListener server;
    private readonly List<GameClient> connectedClients
        = new List<GameClient>();

    private readonly ConcurrentQueue<Packet> availablePackets
        = new ConcurrentQueue<Packet>();

    public GameServer()
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 1337);
        this.server = new TcpListener(ipEndPoint);
    }

    public bool IsBound => this.server.Server.IsBound;

    public void Start()
    {
        try
        {
            this.server.Start(0x1000);
            this.server.BeginAcceptTcpClient(OnAcceptTcpClient, null);
            Debug.Log("Server started");
        }
        catch (Exception exc)
        {
            System.IO.File.AppendAllText("exceptions.log", exc.ToString());
            throw exc;
        }
    }


    public Packet ReadPacket()
    {
        if (this.availablePackets.TryDequeue(out var packet))
        {
            Debug.Log("Read packet: " + packet.Command);
            return packet;
        }
        return null;
    }

    private void OnAcceptTcpClient(IAsyncResult ar)
    {
        try
        {
            var tcpClient = this.server.EndAcceptTcpClient(ar);
            this.ClientConnected(tcpClient);
        }
        catch { }
        try
        {
            this.server.BeginAcceptTcpClient(OnAcceptTcpClient, null);
        }
        catch { }
    }

    private void ClientConnected(TcpClient client)
    {
        Debug.Log("Client connected");
        this.connectedClients.Add(new GameClient(this, client));
    }

    public void Stop()
    {
        if (this.server.Server.IsBound)
        {
            this.server.Stop();
        }
    }

    public void Dispose()
    {
        this.Stop();
    }

    public void DataReceived(GameClient gameClient, string cmd)
    {
        availablePackets.Enqueue(new Packet(gameClient, cmd));
    }
}