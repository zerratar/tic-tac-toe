using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class GameClient : IDisposable
{
    private readonly GameServer server;
    private readonly TcpClient client;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;

    private readonly ConcurrentQueue<string> toWrite
        = new ConcurrentQueue<string>();

    public GameClient(GameServer server, TcpClient client)
    {
        this.server = server;
        this.client = client;

        reader = new StreamReader(this.client.GetStream());
        writer = new StreamWriter(this.client.GetStream());

        this.BeginReceive();
        this.BeginWrite();
    }

    private async Task BeginReceive()
    {
        while (client.Connected)
        {
            //var sb = new StringBuilder();
            var msg = "";
            try
            {

                msg = await this.reader.ReadLineAsync();

                //var buffer = new char[4096];
                //var read = 0;
                //while ((read = await this.reader.ReadAsync(buffer, 0, buffer.Length)) != 0)
                //{
                //    sb.Append(buffer, 0, read);
                //}
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.ToString());
            }
            Debug.WriteLine(msg);
            this.HandlePacket(msg);
        }

    }

    private async Task BeginWrite()
    {
        while (client.Connected)
        {
            try
            {
                if (toWrite.TryDequeue(out var cmd))
                {
                    Debug.WriteLine(cmd);
                    await this.writer.WriteLineAsync(cmd);
                    await this.writer.FlushAsync();
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.ToString());
            }
        }

    }

    public void Write(string cmd)
    {
        toWrite.Enqueue(cmd);
    }

    private void HandlePacket(string cmd)
    {
        server.DataReceived(this, cmd);
    }

    public void Dispose()
    {
        try
        {
            client?.Dispose();
            reader?.Dispose();
            writer?.Dispose();
        }
        catch { }
    }
}