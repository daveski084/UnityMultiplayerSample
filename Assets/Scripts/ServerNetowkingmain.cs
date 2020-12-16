﻿using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor;

public class ServerNetowkingmain : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;

    List<PlayerSpawnMsg> AllSpawnMsg = new List<PlayerSpawnMsg>();

    void Start(){

        Debug.Log("Initialized.");
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;

        if (m_Driver.Bind(endpoint) != 0){
            Debug.Log("Failed to bind to port " + serverPort);
        }
        else {
            m_Driver.Listen();
        }

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        InvokeRepeating("SendHandShake", 0.0f, 2.0f);
    }

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }
    public void OnDestroy() {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c) {
        SendIDToClient(c);
        SendAllSpawnedPlayers(c);
        m_Connections.Add(c);
        Debug.Log("Connection established");
    }

    void SendIDToClient(NetworkConnection c){
        RequestIDMsg m = new RequestIDMsg();
        m.ID = c.InternalId.ToString();
        SendToClient(JsonUtility.ToJson(m), c);
    }

    void SendAllSpawnedPlayers(NetworkConnection c) {
        foreach (PlayerSpawnMsg msg in AllSpawnMsg) {
            SendToClient(JsonUtility.ToJson(msg), c);
        }
    }

    void SendHandShake(){
        foreach (NetworkConnection c in m_Connections){
            HandshakeMsg m = new HandshakeMsg();
            m.player.id = c.InternalId.ToString();
            SendToClient(JsonUtility.ToJson(m), c);
        }
    }

    void SpawnNewPlayer(PlayerSpawnMsg msg){
        foreach (NetworkConnection c in m_Connections){
            SendToClient(JsonUtility.ToJson(msg), c);
        }
    }

    void UpdatePlayerStats(UpdateStatsMsg msg){
        foreach (NetworkConnection c in m_Connections){
            SendToClient(JsonUtility.ToJson(msg), c);
        }
    }

    void DisconnectPlayer(PlayerDisconnectMsg msg){
        foreach (NetworkConnection c in m_Connections){
            SendToClient(JsonUtility.ToJson(msg), c);
        }
    }

    void OnData(DataStreamReader stream, int i){
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

            case Commands.PLAYER_SPAWN:
                PlayerSpawnMsg psMsg = JsonUtility.FromJson<PlayerSpawnMsg>(recMsg);
                AllSpawnMsg.Add(psMsg);
                SpawnNewPlayer(psMsg);
                break;

            case Commands.UPDATE:
                UpdateStatsMsg usMsg = JsonUtility.FromJson<UpdateStatsMsg>(recMsg);
                UpdatePlayerStats(usMsg);
                break;

            case Commands.PLAYER_DISCONNECT:
                PlayerDisconnectMsg pdMsg = JsonUtility.FromJson<PlayerDisconnectMsg>(recMsg);
                AllSpawnMsg.Remove(FindPlayerSpawnMsg(pdMsg.PlayerID));
                DisconnectPlayer(pdMsg);
                break;

            default:
                Debug.Log("This is the default case : Server side!");
                break;
        }
    }


    PlayerSpawnMsg FindPlayerSpawnMsg(string ID){
        foreach (PlayerSpawnMsg msg in AllSpawnMsg){
            if (msg.ID == ID){
                return msg;
            }
        }
        return null;
    }

    void OnDisconnect(int i){
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update(){

        m_Driver.ScheduleUpdate().Complete();

        for (int i = 0; i < m_Connections.Length; i++){
            
            if (!m_Connections[i].IsCreated){

                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        
        NetworkConnection c = m_Driver.Accept();
        while (c != default(NetworkConnection)){
            
            OnConnect(c);
            c = m_Driver.Accept();
        }


        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++){

            Assert.IsTrue(m_Connections[i].IsCreated);
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty){

                if (cmd == NetworkEvent.Type.Data){
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect){
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}
