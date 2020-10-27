using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Network;
using Network.Enums;
using Network.Packets;
using Network.RSA;
using static LTServer.LicznikTransferu;

namespace LTServer
{
    class LTServer
    {
        short port;
        ServerConnectionContainer container;

        LicznikTransferu nextLt;

        bool ltBusy = false;
        async void InitNextLT()
        {
            while(ltBusy)
            {
                await Task.Delay(1);
            }

            ltBusy = true;

            nextLt = new LicznikTransferu();

            ltBusy = false;
        }

        async Task<LicznikTransferu> InitLT()
        {
            await Task.Delay(1);
            LicznikTransferu lt = null;
            if(!ltBusy)
            {
                ltBusy = true;

                if (nextLt == null)
                {
                    InitNextLT();
                    ltBusy = false;
                }
                else
                {
                    lt = nextLt;
                    nextLt = null;
                    InitNextLT();
                    ltBusy = false;
                }

                
            }
            if (lt == null) 
            {
                lt = new LicznikTransferu();
            }
            return lt;
        }

        void GetLimitHandler(RawData data, Connection con)
        {
            try
            {
                Task<LicznikTransferu> initTask = InitLT();
                for(int i = 0;!initTask.IsCompleted;i++)
                {
                    Debug.Log("Waiting for the task to be completed...");
                    Thread.Sleep(100);
                    if(i%5 == 0)
                    {
                        con.SendRawData(data.Key, Encoding.UTF8.GetBytes("wait"));
                    }
                }
                con.SendRawData(data.Key, Encoding.UTF8.GetBytes("ok"));

                LicznikTransferu licznik = initTask.GetAwaiter().GetResult();

                string response = "UE;Nieznany blad";

                Debug.Log("[GetLimitHandler]");
                string info = Encoding.UTF8.GetString(data.Data);
                string[] creds = info.Split(';');
                if (creds.Length != 2)
                {
                    response = "BD;Blad dekodowania danych logowania";
                }
                else
                {
                    var limits = licznik.GetDownloadLimits(creds[0], creds[1]);//licznik.GetDownloadLimits(creds[0], creds[1]);
                    if (limits.cost == DownloadLimits.EMPTY.cost)
                    {
                        Debug.Log("Bledny login lub haslo");
                        response = "BL;Bledny login lub haslo";
                    }
                    else
                    {
                        response = limits.ToString();
                    }
                }
                
                Debug.Log("Responding to request");
                con.SendRawData(data.Key, Encoding.UTF8.GetBytes(response));

                licznik.Close();
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[GetLimitHandler] " + con.IPRemoteEndPoint.Address.ToString());
                try
                {
                    con.SendRawData(data.Key, Encoding.UTF8.GetBytes("SC;Wewnetrzny blad servera"));
                }
                catch(Exception e2)
                {
                    Debug.Exception(e2, "[Responding after crash]");
                }
            }
        }


        void ConnectionEstablished(Connection con, ConnectionType type)
        {
            //liczniki.Add(con, new LicznikTransferu());


            con.RegisterRawDataHandler("getLimit", GetLimitHandler);
            Debug.Log("Connection established");
        }

        private void Container_ConnectionLost(Connection con, ConnectionType arg2, CloseReason arg3)
        {      
            con.UnRegisterRawDataHandler("getLimits");
            Debug.Log("Connection lost");
        }


        void InitServer()
        {
            container = ConnectionFactory.CreateServerConnectionContainer(port);
            //var keys = RSAKeyGeneration.Generate(512);
            //container = ConnectionFactory.CreateSecureServerConnectionContainer(port, keys);

            #region Optional settings
            container.ConnectionEstablished += ConnectionEstablished;
            container.ConnectionLost += Container_ConnectionLost;
            container.AllowUDPConnections = false;
            #endregion

            container.StartTCPListener();

            Debug.Log("Server started");
        }

        public void Join()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
            catch(Exception e)
            {
                Debug.Exception(e, "[MainThreadSleep]");
            }
        }

        public LTServer(short port)
        {
            this.port = port;

            //licznik = new LicznikTransferu();
            InitServer();
        }
    }
}
