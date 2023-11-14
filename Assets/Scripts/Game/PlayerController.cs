using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public uint ID { get => _id; set => _id = value; }
    private uint _id = 0;
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
                DebugManager.AddLog("Add GameObject with error");
            }
        }
    }
}
