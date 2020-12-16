using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands{
        PLAYER_UPDATE,
        PLAYER_DISCONNECT,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT,
        PLAYER_SPAWN,
        REQUEST_ID,
        UPDATE
    }

    [System.Serializable]
    public class NetworkHeader{
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg(){
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class RequestIDMsg : NetworkHeader{
        public string ID;
        public RequestIDMsg(){    
            cmd = Commands.REQUEST_ID;
        }
    }

    [System.Serializable]
    public class UpdateStatsMsg : NetworkHeader{
        public string ID;
        public Vector3 Position;
        public UpdateStatsMsg(){    
            cmd = Commands.UPDATE;
        }
    }

    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public PlayerUpdateMsg(){     
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }
    };

    [System.Serializable]
    public class PlayerDisconnectMsg : NetworkHeader {
        public string PlayerID;
        public PlayerDisconnectMsg(){      
            cmd = Commands.PLAYER_DISCONNECT;
        }
    };

    [System.Serializable]
    public class PlayerSpawnMsg:NetworkHeader{
        public Vector3 Position;
        public string ID;
        public PlayerSpawnMsg(){      
            cmd = Commands.PLAYER_SPAWN;
        }
    };


    public class PlayerInputMsg:NetworkHeader {
        public Input myInput;
        public PlayerInputMsg() {
            cmd = Commands.PLAYER_INPUT;
            myInput = new Input();
        }
    }

    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader {
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg() {      
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
} 

namespace NetworkObjects{
    [System.Serializable]
    public class NetworkObject {
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject {
        public Color cubeColor;
        public Vector3 cubPos;

        public NetworkPlayer(){
            cubeColor = new Color();
        }
    }
}