using System;
using UnityEngine;

namespace WSClientSample
{
	public class GameDataStoreInitializer : MonoBehaviour
	{
		public GameDataStore GameDataStore;
		private float reconnectionTimer = 0;
		private void Awake()
		{
			GameDataStore.InitAndConnect();
		}

		void Update()
		{
#if !UNITY_WEBGL || UNITY_EDITOR
			GameDataStore.DispatchMessageQueue();
#endif
			if (GameDataStore.ConnectionStatus == ConnectionStatus.Disconnected)
			{
				reconnectionTimer += Time.deltaTime;
				if (reconnectionTimer > GameDataStore.reconnectionDelay)
				{
					GameDataStore.InitAndConnect();
					reconnectionTimer = 0;
				}
			}
		}
		
		private void OnDestroy()
		{
			GameDataStore.Stop();
		}

		private void OnApplicationQuit()
		{
			GameDataStore.Stop();
		}
	}
}