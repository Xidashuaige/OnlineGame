using TMPro;
using UnityEngine;

public class BirdController : MonoBehaviour
{
    private uint _netId = 0;
    public uint NetId { get => _netId; set => _netId = value; }
    public bool Owner { get => _owner; set => _owner = value; }
    [SerializeField] private bool _owner = false;

    private BirdMovement _movement;

    public void InitBirdController()
    {
        if (_movement != null)
            return;

        _movement = GetComponent<BirdMovement>();

        _movement.InitMovement();

        if (Owner)
        {
            _movement.onBirdMove += OnBirdMove;       
        }
        else
        {
            var _ = GetComponent<SpriteRenderer>();
            _.color = new(1, 1, 1, 0.5f);
        }
    }


    private void OnBirdMove(Vector2 position, bool flipX, float timeUsed)
    {
        Client.Instante.RequestMoveBird(NetId, position, flipX, timeUsed);
    }

    public void SetPosition(Vector2 position, bool flipX, float timeUsed)
    {
        _movement.SetFuturePos(position, timeUsed);
        _movement.SetFlip(flipX);
    }

}
