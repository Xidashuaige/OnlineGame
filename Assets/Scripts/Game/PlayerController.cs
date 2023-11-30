using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private uint _netId = 0;
    public uint NetId { get => _netId; set => _netId = value; }
    private PlayerMovement _movement;

    public bool Owner { get => _owner; set => _owner = value; }
    [SerializeField] private bool _owner = false;

    public void InitPlayerController()
    {
        if (_movement != null)
            return;

        _movement = GetComponent<PlayerMovement>();

        if (Owner)
            _movement.onPlayerMove += OnPlayerMove;
    }


    private void OnDestroy()
    {
        if (_movement != null)
            _movement.onPlayerMove -= OnPlayerMove;
    }

    private void OnPlayerMove(Vector2 position)
    {
        Debug.Log("move");

        Client.Instante.RequestMovePlayer(NetId, position);
    }
}
