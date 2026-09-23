using TMPro;
using UnityEngine;
using WSClientSample;

public class UIUpdateTextWithClientID : MonoBehaviour
{
    public GameDataStore _data;
    private string _id;

    private TMP_Text _text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_id != _data.MyData.clientID)
        {
            _id = _data.MyData.clientID;
            _text.text = _id;
        }
    }
}
