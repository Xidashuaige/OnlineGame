using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1;

    private PlayerController _playerController;

    private Vector2 _moveInput = new();

    private Rigidbody2D _rb;

    public Action<Vector2> onPlayerMove = null;

    // Start is called before the first frame update
    void Start()
    {
        _playerController = GetComponent<PlayerController>();

        if (_playerController.Owner)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
        }
        else
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_playerController.Owner)
        {
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            //_moveInput.y = Input.GetAxis("Vertical");
        }
    }

    private void FixedUpdate()
    {
        if (_playerController.Owner)
        {
            _rb.velocity = new Vector2(_moveInput.x * _moveSpeed, _rb.velocity.y);

            if (_rb.velocity != Vector2.zero)
                onPlayerMove?.Invoke(transform.position);
        }
    }
}
