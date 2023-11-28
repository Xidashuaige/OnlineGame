using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    [SerializeField] private Server _server;
    private List<ClientInfo> _clientsInTheGame = null;

    public void InitGame(List<ClientInfo> clients)
    {
        _clientsInTheGame = clients;
    }
}
