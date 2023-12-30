using System;
using UnityEngine;

public class BirdMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1.0f;

    private BirdController _birdController;

    private Vector2 _moveInput = new();

    private Rigidbody2D _rb;

    public Action<Vector2, bool, float> onBirdMove = null;

    private SpriteRenderer _spriteRenderer;

    // for another players
    private float frameCount = 0;

    // for me
    private Vector2 futurePos;
    private float timeUsed;

    public void InitMovement()
    {
        _birdController = GetComponent<BirdController>();

        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_birdController.Owner)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;

            // Start to move        
            _moveInput.x = UnityEngine.Random.value > 0.5 ? 1 : -1;
            _spriteRenderer.flipX = _moveInput.x > 0;
        }
        else
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.InGame)
            return;

        if (_birdController == null || !_birdController.Owner)
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
            if (Input.GetKey(KeyCode.D))
                BirdMove(Vector2.right, true, 3.0f);
            else if (Input.GetKey(KeyCode.A))
                BirdMove(Vector2.left, false, 3.0f);

            if (Input.GetKeyUp(KeyCode.D))
                BirdMove(Vector2.right, true, 1.0f);
            else if (Input.GetKeyUp(KeyCode.A))
                BirdMove(Vector2.left, false, 1.0f);

            // Send position to another player every 0.075s
            if ((frameCount += Time.deltaTime) >= 0.075f)
            {
                onBirdMove?.Invoke(transform.position, _spriteRenderer.flipX, frameCount);
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

    private void BirdMove(Vector2 dir, bool flip, float speed)
    {
        _moveInput = dir;
        _spriteRenderer.flipX = flip;
        _moveSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (_birdController == null || !_birdController.Owner)
            return;

        _rb.velocity = new Vector2(_moveInput.x * _moveSpeed, _rb.velocity.y);
    }

    public void SetFuturePos(Vector2 position, float timeUsed)
    {
        futurePos = position;
        this.timeUsed = timeUsed;
    }

    public void SetFlip(bool flipX)
    {
        _spriteRenderer.flipX = flipX;
    }
}
