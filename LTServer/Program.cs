using System;

using Network;

namespace LTServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Log("Starting server...");

            LTServer server = new LTServer(7154);
            server.Join();
        }
    }
}
