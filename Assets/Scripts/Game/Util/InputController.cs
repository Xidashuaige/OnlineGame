using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    private TMP_InputField _input;
    private Image _img;

    public string Value
    {
        get
        {
            if (_input.text == "")
                StartCoroutine(FlashRed());

            return _input.text;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _input = GetComponent<TMP_InputField>();
        _img = GetComponent<Image>();
    }

    private IEnumerator FlashRed()
    {
        float fadeSpeed = 0.02f;

        for (float t = 0.0f; t < 1.0f; t += fadeSpeed)
        {
            _img.color = Color.Lerp(Color.white, Color.red, t);

            yield return null;
        }

        for (float t = 0.0f; t < 1.0f; t += fadeSpeed)
        {
            _img.color = Color.Lerp(Color.red, Color.white, t);

            yield return null;
        }
    }
}
