using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

public class ClientGameNetworking : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;
    public string PlayerID;
    NetInfo playerInfo;
    public GameObject PlayerPrefab;
    public GameObject playerGO;

    [SerializeField]
    List<GameObject> AllPlayersGO = new List<GameObject>();


    void Start(){
        Debug.Log("Initialized.");
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }

    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("Connected to the server now.");
        PlayerID = UnityEngine.Random.value.ToString();
        SpawnPlayer();
        InvokeRepeating("SendHandShake", 0.0f, 2.0f);
        InvokeRepeating("UpdateStats", 0.0f, 1.0f / 30.0f);
    }
    

    void SpawnPlayer(){

        Vector3 pos = new Vector3(UnityEngine.Random.Range(-2.0f, 2.0f), 0.0f, 0.0f);
        playerGO = Instantiate(PlayerPrefab, pos, new Quaternion());
        playerInfo = playerGO.GetComponent<NetInfo>();
        playerGO.GetComponent<NetInfo>().playerID = PlayerID;
        playerInfo.playerID = PlayerID;
        AllPlayersGO.Add(playerGO);
     
        PlayerSpawnMsg msg = new PlayerSpawnMsg();
        msg.Position = pos;
        msg.ID = PlayerID;
        SendToServer(JsonUtility.ToJson(msg));
    }

    void SpawnOtherPlayer(PlayerSpawnMsg msg){
        
        if (msg.ID != PlayerID){
            GameObject otherPlayerGO = Instantiate(PlayerPrefab, msg.Position, new Quaternion());
            otherPlayerGO.GetComponent<NetInfo>().playerID = msg.ID;
            AllPlayersGO.Add(otherPlayerGO);
        }
        
    }

    void UpdateOtherPlayer(UpdateStatsMsg msg) {
        
        if (msg.ID != PlayerID){
            GameObject Obj = FindPlayerObj(msg.ID);
            if (Obj){
                Obj.transform.position = msg.Position;
            }
        }
    }

    GameObject FindPlayerObj(string ID){
        
        foreach (GameObject go in AllPlayersGO){
            if (go.GetComponent<NetInfo>().playerID == ID) {
                return go;
            }
        }
        return null;
    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd){
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                break;

            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                break;

            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                break;

            case Commands.REQUEST_ID:
                Debug.Log("Requesting ID"); 
                RequestIDMsg riMsg = JsonUtility.FromJson<RequestIDMsg>(recMsg);
                playerInfo.serverID = riMsg.ID;
                break;

            case Commands.PLAYER_SPAWN:
                Debug.Log("Player spawn!");
                PlayerSpawnMsg psMsg = JsonUtility.FromJson<PlayerSpawnMsg>(recMsg);
                SpawnOtherPlayer(psMsg);
                break;

            case Commands.UPDATE:
                UpdateStatsMsg usMsg = JsonUtility.FromJson<UpdateStatsMsg>(recMsg);
                UpdateOtherPlayer(usMsg);
                break;

            case Commands.PLAYER_DISCONNECT:
                Debug.Log("Player disconnected.");
                PlayerDisconnectMsg pdMsg = JsonUtility.FromJson<PlayerDisconnectMsg>(recMsg);
                DestroyPlayer(pdMsg);
                break;

            default:
                Debug.Log("This is the default case.");
                break;
        }
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from the server.");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy(){
        m_Driver.Dispose();
    }

    void SendHandShake() {
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = m_Connection.InternalId.ToString();
        SendToServer(JsonUtility.ToJson(m));
    }

    void DestroyPlayer(PlayerDisconnectMsg msg){
        Destroy(FindPlayerObj(msg.PlayerID));
    }

    void Dissconnect() {
        PlayerDisconnectMsg m = new PlayerDisconnectMsg();
        m.PlayerID = PlayerID;
        SendToServer(JsonUtility.ToJson(m));
    }

    void UpdateStats() {
        UpdateStatsMsg m = new UpdateStatsMsg();
        m.ID = PlayerID;
        m.Position = playerGO.transform.position;
        SendToServer(JsonUtility.ToJson(m));
    }

    void Update(){
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated){
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty){
            if (cmd == NetworkEvent.Type.Connect){
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data) { 
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect){
                Dissconnect();
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }

        if (Input.GetKeyDown(KeyCode.Escape)){
            Dissconnect();
            Invoke("Exit", 1.0f);
        }

    }

    void Exit(){
        Application.Quit();
    }
}
