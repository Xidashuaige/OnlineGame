using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private uint _netId = 0;
    public uint NetId { get => _netId; set => _netId = value; }
    private PlayerMovement _movement;

    public bool Owner { get => _owner; set => _owner = value; }
    [SerializeField] private bool _owner = false;

    private void OnEnable()
    {
        if (_movement != null)
            return;

        _movement = GetComponent<PlayerMovement>();

        if (Owner)
        {
            try
            {
                GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>().AddPlayer(this);
            }
            catch
            {
                Debug.Log("Add GameObject with error");
            }
        }
    }
}
