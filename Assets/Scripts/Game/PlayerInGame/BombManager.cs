using System.Collections.Generic;
using UnityEngine;

public class BombManager : MonoBehaviour
{
    [SerializeField] private GameObject _bombPrefab;
    private Dictionary<uint, Bomb> _bombs = new();
    private Stack<Bomb> _bombsWithoutId = new();

    // Start is called before the first frame update
    void Start()
    {
        Client.Instance.onActionHandlered[NetworkMessageType.UpdateBombPosition] += OnUpdateBombPosition;
        Client.Instance.onActionHandlered[NetworkMessageType.Explotion] += OnExplotion;
    }

    public void CreateBomb(Vector2 pos, uint netId = 0, bool owner = true)
    {
        GameObject bomb = Instantiate(_bombPrefab, transform);

        bomb.transform.position = pos;

        var _ = bomb.GetComponent<Bomb>();

        _.NetId = netId;

        _.Owner = owner;

        _.InitBomb();

        if (owner)
            _bombsWithoutId.Push(_);
        else
            _bombs.Add(_.NetId, _);
    }

    private void OnUpdateBombPosition(NetworkMessage data)
    {
        var message = data as UpdateBombMovement;

        if (!_bombs.ContainsKey(message.netId))
        {
            // If is our bomb
            if (message.messageOwnerId == Client.Instance.ID)
            {
                if (_bombsWithoutId.Count > 0)
                {
                    var _ = _bombsWithoutId.Pop();

                    _.NetId = message.netId;

                    _bombs.Add(_.NetId, _);
                }
            }
            // If is bomb by another player 
            else
            {
                CreateBomb(message.position, message.netId, false);
            }
        }
        else if(message.messageOwnerId != Client.Instance.ID)
        {
            _bombs[message.netId].SetPosition(message.position);
        }      
    }

    private void OnExplotion(NetworkMessage data)
    {
        var message = data as ExplotionMessage;

        _bombs[message.netId].SetPosition(message.position);

        _bombs[message.netId].Explotion();
    }
}
