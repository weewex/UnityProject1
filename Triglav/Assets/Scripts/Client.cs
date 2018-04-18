using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player
{
    public string playerName;
    public GameObject avatar;
    public int connectionId;
}


public class Client : MonoBehaviour {

    private const int MAX_CONNECTION = 100;

    private int port = 25001;

    private int hostId;
    private int webHostId;

    private int reliableChannel;
    private int unreliableChannel;

    private int ourClientId;
    private int connectionId;

    private float connectionTime;
    private bool isConnected = false;
    private bool isStarted = false;
    private byte error;

    private string playerName;
    public GameObject playerPrefab;
    public Dictionary<int,Player> players = new Dictionary<int, Player>();

    public void Connect()
    {
        string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if(pName == "")
        {
            Debug.Log("You must enter a name");
            return;
        }
        playerName = pName;

        string IP = GameObject.Find("IPInput").GetComponent<InputField>().text;
        if (IP == "")
        {
            Debug.Log("You must enter a an IP");
            return;
        }


        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, 0);
        connectionId = NetworkTransport.Connect(hostId, IP, port, 0, out error);

        connectionTime = Time.time;

        isConnected = true;
    }

    private void Update()
    {
        if (!isConnected)
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
                break;
            case NetworkEventType.DataEvent:       //3
                string msg = Encoding.ASCII.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving: "+msg);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "ASKNAME":
                        OnAskName(splitData);
                        break;
                    case "CNN":
                        SpawnPlayer(splitData[2], int.Parse(splitData[1]));
                        break;
                    case "DC":
                        PlayerDisconnected(int.Parse(splitData[1]));
                        break;
                    case "ASKPOSITION":
                        OnAskPosition(splitData);
                        break;
                    default:
                        Debug.Log("Invalid message: " + msg);
                        break;
                }

                break;
            case NetworkEventType.DisconnectEvent: //4
                break;
        }
    }

    private void OnAskName(string[] data)
    {
        // set this client id
        ourClientId = int.Parse(data[1]);

        // Send our name to the server
        Send("NAMEIS|" + playerName, reliableChannel);
        //create all other players
        for (int i = 2; i < data.Length-1; i++)
        {
            string[] d = data[i].Split('%');
            SpawnPlayer(d[0], int.Parse(d[1]));
        }
    }

    private void OnAskPosition(string[] data)
    {
        if (!isStarted)
            return;
        
        for (int i = 1; i < data.Length; i++)
        {
            string[] d = data[i].Split('%');
            //int clientId = int.Parse(d[0]);
            int clientId;
            bool success = int.TryParse(d[0], out clientId);
            if (!success)
            {
                Debug.Log("value is : " + d[0]);

            }
            else
            {
                Debug.Log(data[i]);
                if (ourClientId != clientId)
                {
                    Vector3 position = Vector3.zero;
                    position.x = float.Parse(d[1]);
                    position.y = float.Parse(d[2]);
                    position.z = float.Parse(d[3]);
                    players[clientId].avatar.transform.position = position;
                }
            }
        }
        Vector3 myPos = players[ourClientId].avatar.transform.position;
        string m = "MYPOSITION|" + myPos.x.ToString() + '%' + myPos.y.ToString() + '%' + myPos.z.ToString();
        Send(m, unreliableChannel);
    }

    private void SpawnPlayer(string playerName, int cnnId)
    {
        GameObject go = Instantiate(playerPrefab) as GameObject;

        if(cnnId == ourClientId)
        {
            go.AddComponent<PlayerMotor>();
            GameObject.Find("Canvas").SetActive(false);
            isStarted = true;

        }

        Player p = new Player();
        p.avatar = go;
        p.playerName = playerName;
        p.connectionId = cnnId;
        p.avatar.GetComponentInChildren<TextMesh>().text = playerName;
        players.Add(cnnId,p);
    }

    private void PlayerDisconnected(int cnnId)
    {
        Destroy(players[cnnId].avatar);
        players.Remove(cnnId);
    }

    private void Send(string message, int channelId)
    {
        Debug.Log("Sending: " + message);
        byte[] msg = Encoding.ASCII.GetBytes(message);
        NetworkTransport.Send(hostId, connectionId, channelId, msg, message.Length * sizeof(char), out error);
        
    }
}
