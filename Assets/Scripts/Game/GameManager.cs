using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Player
{
    public int id;
    public Vector2 position;
}

public class GameManager : MonoBehaviour
{
    private List<Player> _players;

    // Start is called before the first frame update
    void Start()
    {
        _players = new List<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
