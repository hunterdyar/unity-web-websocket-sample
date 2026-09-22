namespace ws_server_web.Datashare;

//a datastore is the container for all of our databases ('game data')
//so we can have multiple sessions running from one server.

//this class is basically a wrapper around a dictionary of 'GameData's (and you can replace that type, but not worry about this one changing.

public static class DataStoreHub
{
	public static string[] AllStores => DataStores.Keys.ToArray();
	public static Action<string,int> OnConnectionChanged;
	public static Dictionary<string, int> Connections = new Dictionary<string, int>();
	public static Dictionary<string, GameData> DataStores = new Dictionary<string, GameData>();

	public static bool TryGetDataStore<T>(string storeid, out T store)
	{
		if(DataStores.TryGetValue(storeid, out var s))
		{
			if (s is T dstore)
			{
				 store = dstore;
				 return true;
			}
		}

		store = default(T);
		return false;
	}

	public static void CreateDataStore(string id, GameData store)
	{
		DataStores.Add(id,store);
	}

	public static void ConnectionDelta(string storeId, int delta)
	{
		if (Connections.ContainsKey(storeId))
		{
			Connections[storeId] += delta;
		}
		else
		{
			Connections.Add(storeId,delta);
		}
		OnConnectionChanged?.Invoke(storeId,Connections[storeId]);
	}

	public static int GetConnectionCount(string item)
	{
		if (Connections.TryGetValue(item, out var val))
		{
			return val;
		}
		else
		{
			return 0;
		}
	}
}