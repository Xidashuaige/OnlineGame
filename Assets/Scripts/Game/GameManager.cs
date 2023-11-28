using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    private List<ClientInfo> _clientsInTheGame = null;

    public void InitGame(List<ClientInfo> clients)
    {
        _clientsInTheGame = clients;
    }
}
