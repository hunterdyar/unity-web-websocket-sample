using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using WSClientSample;

namespace WSClient.Scripts.Sample
{
	public class UIMessageField : MonoBehaviour
	{
		public GameDataStore _data;
		private string _lastUpdatedValue;
		private TMP_InputField _inputField;
		
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Awake()
		{
			_inputField = GetComponent<TMP_InputField>();
		}

		private void OnEnable()
		{
			_inputField.onValueChanged.AddListener(OnValueChanged);
		}

		private void OnValueChanged(string value)
		{
			if (_lastUpdatedValue != value)
			{
				_data.UpdateClientMessage(value);
				_lastUpdatedValue = value;
			}
		}
		
	}
}