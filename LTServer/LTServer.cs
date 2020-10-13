using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Network;
using Network.Enums;
using Network.Packets;
using static LTServer.LicznikTransferu;

namespace LTServer
{
    class LTServer
    {
        short port;
        ServerConnectionContainer container;

        LicznikTransferu licznik;
        
        void GetLimitHandler(RawData data, Connection con)
        {
            string response = "UE;Nieznany blad";

            Debug.Log("[GetLimitHandler]");
            string info = Encoding.UTF8.GetString(data.Data);
            string[] creds = info.Split(';');
            if(creds.Length != 2)
            {
                response = "BD;Blad dekodowania danych logowania";
            }
            else
            {
                var limits = licznik.GetDownloadLimits(creds[0], creds[1]);
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
            

            con.SendRawData(data.Key, Encoding.UTF8.GetBytes(response));
        }

        void ConnectionEstablished(Connection con, ConnectionType type)
        {
            con.RegisterRawDataHandler("getLimit", GetLimitHandler);
            Debug.Log("Connection established");
        }

        private void Container_ConnectionLost(Connection arg1, ConnectionType arg2, CloseReason arg3)
        {
            Debug.Log("Connection lost");
        }


        void InitServer()
        {
            container = ConnectionFactory.CreateServerConnectionContainer(port);

            #region Optional settings
            container.ConnectionEstablished += ConnectionEstablished;
            container.ConnectionLost += Container_ConnectionLost;
            #endregion

            container.StartTCPListener();

            Debug.Log("Server started");
        }

        public void Join()
        {
            while(true)
            {
                Thread.Sleep(1000);
            }
        }

        public LTServer(short port)
        {
            this.port = port;

            licznik = new LicznikTransferu();
            InitServer();
        }
    }
}
