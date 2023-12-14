using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = .5f;

    private PlayerController _playerController;

    private Vector2 _moveInput = new();

    private Rigidbody2D _rb;

    public Action<Vector2, bool, float> onPlayerMove = null;

    private SpriteRenderer _spriteRenderer;

    // for another players
    private float frameCount = 0;

    // for me
    private Vector2 futurePos;
    private float timeUsed;

    public void InitMovement()
    {
        _playerController = GetComponent<PlayerController>();

        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_playerController.Owner)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.freezeRotation = true;

            // Start to move        
            _moveInput.x = UnityEngine.Random.value > 0.5 ? 1 : -1;
            _spriteRenderer.flipX = _moveInput.x > 0;
        }
        else
        {
            GetComponent<CapsuleCollider2D>().isTrigger = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_playerController == null || !_playerController.Owner)
        {
            // Update position if isn' owner
            if (futurePos != null)
            {
                float t = Time.deltaTime / timeUsed;

                if (t < 1.0f)
                {
                    transform.position = Vector2.Lerp(transform.position, futurePos, t);

                    timeUsed -= (Time.deltaTime * 1.5f);
                }
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.RightArrow))
                PlayerMove(Vector2.right, true, 2);
            else if (Input.GetKey(KeyCode.LeftArrow))
                PlayerMove(Vector2.left, false, 2);

            if (Input.GetKeyUp(KeyCode.RightArrow))
                PlayerMove(Vector2.right, true, .5f);
            else if (Input.GetKeyUp(KeyCode.LeftArrow))
                PlayerMove(Vector2.left, false, .5f);

            // Send position to another player every 0.075s
            if ((frameCount += Time.deltaTime) >= 0.075f)
            {
                onPlayerMove?.Invoke(transform.position, _spriteRenderer.flipX, frameCount);
                frameCount = 0;
            }
        }

        float rayLen = 0.05f;

        int checkLayer = LayerMask.GetMask("Wall");

        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position + _moveInput * 0.4f, _moveInput, rayLen, checkLayer);

        //Debug.DrawRay((Vector2)transform.position + _moveInput * 0.4f, _moveInput * rayLen, Color.red);

        if (hit.collider != null)
        {
            if (hit.collider.gameObject.CompareTag("WallLimit"))
            {
                _moveInput.x *= -1;
                _spriteRenderer.flipX = !_spriteRenderer.flipX;
            }
        }
    }

    private void PlayerMove(Vector2 dir, bool flip, float speed)
    {
        _moveInput = dir;
        _spriteRenderer.flipX = flip;
        _moveSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (_playerController == null || !_playerController.Owner)
            return;

        _rb.velocity = new Vector2(_moveInput.x * _moveSpeed, _rb.velocity.y);
    }

    public void SetFuturePos(Vector2 pos, float timeUsed)
    {
        futurePos = pos;
        this.timeUsed = timeUsed;
    }

    public void SetFlip(bool flipX)
    {
        _spriteRenderer.flipX = flipX;
    }
}
