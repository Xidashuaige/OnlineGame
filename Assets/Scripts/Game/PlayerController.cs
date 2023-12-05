using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private uint _netId = 0;
    public uint NetId { get => _netId; set => _netId = value; }
    private PlayerMovement _movement;

    public bool Owner { get => _owner; set => _owner = value; }
    [SerializeField] private bool _owner = false;

    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _child;
    public void InitPlayerController(string name = "Unknown")
    {
        if (_movement != null)
            return;

        _movement = GetComponent<PlayerMovement>();
        _animator = GetComponent<Animator>();
        _child = transform.GetChild(0).gameObject;

        // Set user name
        var texts = GetComponentsInChildren<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].text = name;
        }

        // Get random avatar for player
        _animator.SetInteger("Player", Random.Range(1, 6));

        if (Owner)
        {
            _movement.onPlayerMove += OnPlayerMove;
            _child.SetActive(true);
        }
    }


    private void OnDestroy()
    {
        if (_movement != null)
            _movement.onPlayerMove -= OnPlayerMove;
    }

    private void OnPlayerMove(Vector2 position, bool flipX, float timeUsed)
    {
        Client.Instante.RequestMovePlayer(NetId, position, flipX, timeUsed);
    }

    public void SetPosition(Vector2 position, bool flipX,float timeUsed)
    {
        _movement.SetFuturePos(position, timeUsed);
        _movement.SetFlip(flipX);
    }
}
