using System;

using Network;

namespace LTServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Log("Starting server...");

            try
            {
                LTServer server = new LTServer(7154);
                server.Join();
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[FatalException - whole app]");
            }
        }
    }
}
