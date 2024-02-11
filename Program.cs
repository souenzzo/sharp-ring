using Net = System.Net;
using Socket = System.Net.Sockets;
using Im = System.Collections.Immutable;
using System.Text;

namespace helloNet;


record HttpRequest(
    Stream? body,
    Im.ImmutableDictionary<String, String> headers,
    String protocol,
    String? queryString,
    String remoteAddr,
    String requestMethod,
    String scheme,
    String serverName,
    int serverPort,
    String? sslClientCert,
    string uri
);

record HttpResponse(
    Stream? Body,
    Im.ImmutableDictionary<String, String>? Headers,
    int Status
);

interface IHttpHandler
{
    HttpResponse Handle(HttpRequest httpRequest);
}

class HttpServer : IDisposable
{
    public static Im.ImmutableDictionary<String, String> ParseHeaders(StreamReader rdr)
    {
        var headers = Im.ImmutableDictionary<String, String>.Empty;
        var readNextHeader = true;
        while (readNextHeader)
        {
            var header = rdr.ReadLine() ?? "";
            if (string.IsNullOrEmpty(header))
            {
                readNextHeader = false;
            }
            else
            {
                var vs = header.Split(":", 2);
                headers = headers.Add(vs.First(), vs.Last());
            }
        }

        return headers;
    }

    private Socket.TcpListener? server;


    public static HttpServer Open(IHttpHandler httpHandler)
    {
        var hs = new HttpServer();

        var localAddr = Net.IPAddress.Parse("127.0.0.1");

        hs.server = new Socket.TcpListener(localAddr, 8080);

        hs.server.Start();


        Console.Write("Waiting for a connection... {0}", hs.server);

        using var client = hs.server.AcceptTcpClient();
        Console.WriteLine("Connected!, {0}", hs.server);
        using var stream = client.GetStream();
        using var rdr = new StreamReader(stream, Encoding.UTF8);
        var requestLine = rdr.ReadLine() ?? "";
        var methodAndOthers = requestLine.Split(" ", 2);
        var requestMethod = methodAndOthers.FirstOrDefault("");
        var uriAndOthers = methodAndOthers.LastOrDefault("").Split(" ", 2);
        var protocol = uriAndOthers.Last();
        var uriAndQuery = uriAndOthers.FirstOrDefault("").Split("?", 2);
        var uri = uriAndQuery.FirstOrDefault("/");
        var queryString = (uriAndQuery.Length == 2) ? uriAndQuery.Last() : null;
        var headers = HttpServer.ParseHeaders(rdr);
        var httpRequest = new HttpRequest(stream, headers, protocol, queryString, "0.0.0.0", requestMethod, "http", "0.0.0.0", 0, null, uri);
        Console.WriteLine("Request: {0}", httpRequest);
        var httpResponse = httpHandler.Handle(httpRequest);
        Console.WriteLine("Response: {0}", httpResponse);
        client.GetStream().Write(Encoding.UTF8.GetBytes("HTTP/1.1 202 OK\r\nfoo: bar\r\ncat: tar\r\n\r\n"));
        client.Close();
        return hs;
    }

    public void Dispose()
    {
        if (server != null)
        {
            server.Dispose();
        }
    }
}

class Program
{
    class MyHandler : IHttpHandler
    {
        public HttpResponse Handle(HttpRequest httpRequest)
        {
            return new HttpResponse(null, null, 200);
        }
    };

    static void Main(string[] args)
    {
        var httpHandler = new MyHandler();
        using var httpServer = HttpServer.Open(httpHandler);
    }
}
