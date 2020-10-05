using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AdvanceMenu : MonoBehaviour
{
    public InputActionAsset controls;
    public string nextScene;

    InputAction any;
    // Start is called before the first frame update
    void Start()
    {
        any = new InputAction(binding: "/*/<button>");
        any.performed += OnAny;

        StartCoroutine(DelayInput());
    }

    private void OnAny(InputAction.CallbackContext obj)
    {
        any.performed -= OnAny;
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    // Update is called once per frame
    IEnumerator DelayInput()
    {
        yield return new WaitForSeconds(1f);
        any.Enable();
    }
}
