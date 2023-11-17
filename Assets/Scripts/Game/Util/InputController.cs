using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _input;
    [SerializeField] private Image _img;

    public string Value
    {
        get
        {
            if (_input.text == "" && gameObject.activeInHierarchy)
                StartCoroutine(FlashRed());

            return _input.text;
        }
    }

    private void Start()
    {
        if (_input == null)
            _input = GetComponent<TMP_InputField>();

        if(_img == null)
            _img = GetComponent<Image>();
    }

    private IEnumerator FlashRed()
    {
        float fadeSpeed = 0.04f;

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
