using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient
{
    public int connectionId;
    public string name;
    public Vector3 position;
}


public class Server : MonoBehaviour {

    private const int MAX_CONNECTION = 100;

    private int port = 25001;

    private int hostId;
    private int webHostId;

    private int reliableChannel;
    private int unreliableChannel;

    private bool isStarted = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();

    private float lastMovementUpdate;
    private float movementUpdateRate = 0.05f;


    private void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, port, null);
        webHostId = NetworkTransport.AddWebsocketHost(topo, port, null);

        isStarted = true;
    }

    private void Update()
    {
        if (!isStarted)
            return;

        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.Nothing:         //1
                break;
            case NetworkEventType.ConnectEvent:    //2
                Debug.Log("Player " + connectionId + "has connected");
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:       //3
                string message = Encoding.ASCII.GetString(recBuffer, 0, dataSize);
                Debug.Log("Player " + connectionId + "has sent : " + message);
                string msg = Encoding.ASCII.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "NAMEIS":
                        OnNameIs(connectionId, splitData[1]);
                        break;

                    case "MYPOSITION":
                        OnMyPosition(connectionId,splitData);
                        break;
                    default:
                        Debug.Log("Invalid message: " + msg);
                        break;
                }

                break;
            case NetworkEventType.DisconnectEvent: //4
                Debug.Log("Player " + connectionId + "has disconnected");
                OnDisconnection(connectionId);
                break;
        }

        // ask player for their position


        if(Time.time - lastMovementUpdate > movementUpdateRate)
        {
            lastMovementUpdate = Time.time;
            string m = "ASKPOSITION|";
            foreach(ServerClient sc in clients)
            {
                m += sc.connectionId.ToString() + '%' + sc.position.x.ToString() + '%' + sc.position.y.ToString()+ '%' + sc.position.z.ToString() + '|';
            }
            m = m.Trim('|');
            Send(m, unreliableChannel, clients);
        }
    }

    private void OnConnection(int cnnId)
    {
        ServerClient c = new ServerClient();
        c.connectionId = cnnId;
        c.name = "TEMP";
        clients.Add(c);


        string msg = "ASKNAME|" + cnnId.ToString() + "|";
        foreach(ServerClient sc in clients)
        {
            msg += sc.name + "%" + sc.connectionId.ToString() + "|";
        }
        msg = msg.Trim('|');

        Send(msg, reliableChannel, cnnId);
    }
    private void OnDisconnection(int cnnId)
    {
        clients.Remove(clients.Find(x => x.connectionId == cnnId));

        Send("DC|" + cnnId, reliableChannel, clients);
    }

    private void OnMyPosition(int cnnId,string[] data)
    {
        string[] d = data[1].Split('%');
        clients.Find(c => c.connectionId == cnnId).position = new Vector3(float.Parse(d[0]), float.Parse(d[1]), float.Parse(d[2]));
    }

    private void OnNameIs(int cnnId, string playerName)
    {
        // link the name to the connection id
        clients.Find(x => x.connectionId == cnnId).name = playerName;

        // tell everyvody that a new player has connected
        Send("CNN|"  + cnnId.ToString() + '|' + playerName, reliableChannel, clients);
        
    }

    private void Send(string message, int channelId, int cnnId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionId == cnnId));
        Send(message, channelId, c);
    }

    private void Send(string message, int channelId, List<ServerClient> c)
    {
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.ASCII.GetBytes(message);
        foreach(ServerClient sc in c)
        {
            NetworkTransport.Send(hostId, sc.connectionId, channelId, msg, message.Length * sizeof(char), out error);
        }
    }
}
