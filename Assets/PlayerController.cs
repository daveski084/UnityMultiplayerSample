using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public ClientGameNetworking client;
    int playerSpeed = 15;
 
   //Move the player
    void Update(){
        if (Input.GetKey(KeyCode.W)){
            
            client.playerGO.transform.position += (Vector3.up * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.S)){
         
            client.playerGO.transform.position += (-Vector3.up * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.A)){
            
            client.playerGO.transform.position += (Vector3.left * Time.deltaTime * playerSpeed);
        }

        if (Input.GetKey(KeyCode.D)){
       
            client.playerGO.transform.position += (-Vector3.left * Time.deltaTime * playerSpeed);
        }
    }
}