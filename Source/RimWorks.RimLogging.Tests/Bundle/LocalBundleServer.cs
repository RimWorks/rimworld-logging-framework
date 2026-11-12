using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RimWorks.RimLogging.Tests.Bundle;

internal sealed class LocalBundleServer : IDisposable
{
    private readonly HttpListener _listener = new HttpListener();
    private readonly Task _loop;

    public readonly List<string> Urls = new List<string>();
    public readonly List<string> Bodies = new List<string>();
    public readonly List<string?> Tokens = new List<string?>();
    public readonly Queue<HttpStatusCode> Statuses = new Queue<HttpStatusCode>();
    public Func<int, string> Body = _ => "{}";

    public LocalBundleServer()
    {
        int port = FreePort();
        Root = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add($"http://*:{port}/");
        _listener.Start();
        _loop = Task.Run(Serve);
    }

    public string Root { get; }

    public string Endpoint => Root + "api/bundle";

    public static string ClosedEndpoint() => $"http://127.0.0.1:{FreePort()}/api/bundle";

    public void Dispose()
    {
        _listener.Close();
        try
        {
            _loop.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
        }
    }

    private static int FreePort()
    {
        TcpListener probe = new TcpListener(IPAddress.Any, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private async Task Serve()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (!_listener.IsListening)
            {
                return;
            }

            int index;
            lock (Urls)
            {
                using StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                Urls.Add(Root.TrimEnd('/') + context.Request.RawUrl);
                Bodies.Add(reader.ReadToEnd());
                Tokens.Add(context.Request.Headers["x-bundle-token"]);
                index = Urls.Count - 1;
            }

            HttpStatusCode status = Statuses.Count > 0 ? Statuses.Dequeue() : HttpStatusCode.OK;
            byte[] bytes = Encoding.UTF8.GetBytes(Body(index));
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            context.Response.Close();
        }
    }
}
