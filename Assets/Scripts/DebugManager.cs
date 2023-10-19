using System.Text;
using TMPro;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    static private TMP_Text _debugMessage;
    [SerializeField] private TMP_InputField _commandInput;
    static private StringBuilder _tempText;

    // Start is called before the first frame update
    void Start()
    {
        _tempText = new StringBuilder();
        _debugMessage = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (_tempText.Length > 0)
        {
            _debugMessage.text += _tempText.ToString();
            _tempText.Clear();
        }
    }

    public static void AddLog(string log)
    {
        log += "\n";

        lock (_tempText)
            _tempText.Append(log);
    }

    public void CommandApply()
    {
        if (_commandInput.text == "cls")
            _debugMessage.text = "";

        _commandInput.text = "";
    }
}
