using System.Collections;
using UnityEngine;

public class BirdController : MonoBehaviour
{
    private uint _netId = 0;
    public uint NetId { get => _netId; set => _netId = value; }
    public bool Owner { get => _owner; set => _owner = value; }
    [SerializeField] private bool _owner = false;
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField] private BombManager _bombManager;

    private bool _canAttack = true;
    private SpriteRenderer _spr;

    private BirdMovement _movement;

    public void InitBirdController(uint netId, bool owner)
    {
        if (_movement != null)
            return;

        NetId = netId;
        Owner = owner;

        _movement = GetComponent<BirdMovement>();

        _movement.InitMovement();

        _spr = GetComponent<SpriteRenderer>();

        if (Owner)
        {
            _movement.onBirdMove += OnBirdMove;

            _bombManager = FindObjectOfType<BombManager>();

            Debug.Log("Init BombManager : " + _bombManager);
        }
        else
        {
            _spr.color = new(1, 1, 1, 0.5f);
        }
    }

    private void Update()
    {
        if (!Owner)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && _canAttack)
        {
            _canAttack = false;
            StartCoroutine(BombCoolDown());
            CreateBomb();
        }
    }

    private IEnumerator BombCoolDown()
    {
        _spr.color = Color.red;

        for (float t = 0; t <= 1.0f; t += 0.05f)
        {
            _spr.color = Color.Lerp(Color.red, Color.white, t);
            yield return new WaitForSeconds(0.1f);
        }

        _canAttack = true;
    }

    private void CreateBomb()
    {
        _bombManager.CreateBomb(transform.position);
    }

    private void OnDestroy()
    {
        if (_movement != null)
            _movement.onBirdMove -= OnBirdMove;
    }

    private void OnBirdMove(Vector2 position, bool flipX, float timeUsed)
    {
        Client.Instance.RequestMoveBird(NetId, position, flipX, timeUsed);
    }

    public void SetPosition(Vector2 position, bool flipX, float timeUsed)
    {
        _movement.SetFuturePos(position, timeUsed);
        _movement.SetFlip(flipX);
    }
}
