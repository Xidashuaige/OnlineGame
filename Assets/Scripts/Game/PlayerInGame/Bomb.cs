using UnityEngine;

public class Bomb : MonoBehaviour
{
    public bool Owner { get; set; }
    public uint NetId { get; set; }

    [SerializeField] private GameObject _explotionPrefab;

    private float _moveInterval = 0;

    public void InitBomb()
    {
        if(Owner)
        {

        }
        else
        {
            var _ = GetComponent<Rigidbody2D>();
            _.gravityScale = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!Owner || !GameManager.Instance.InGame)
            return;

        _moveInterval += Time.deltaTime;
        if (_moveInterval >= 0.05f)
        {
            RequestToMove();
            _moveInterval = 0;
        }
    }

    public void SetPosition(Vector2 pos)
    {
        if (Owner)
            return;

        transform.position = pos;
    }

    public void Explotion()
    {
        Instantiate(_explotionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground"))
        {
            RequestExplotion();
            Explotion();
        }
    }

    private void RequestExplotion()
    {
        Client.Instance.RequestExplotion(NetId, transform.position);
    }

    private void RequestToMove()
    {
        Client.Instance.RequestMoveBomb(NetId, transform.position, 0.05f);
    }
}
