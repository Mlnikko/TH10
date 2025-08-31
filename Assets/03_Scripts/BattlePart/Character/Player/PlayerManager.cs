using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager
{
    public static GameObject CreatePlayer(CharacterConfig characterConfig)
    {
        GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Player/Player");
        GameObject player = GameObject.Instantiate(playerPrefab);
        player.name = "Player";
        CharacterController playerController = player.GetComponent<CharacterController>();
        return player;
    }
}
