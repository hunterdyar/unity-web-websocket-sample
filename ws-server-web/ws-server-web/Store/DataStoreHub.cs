namespace ws_server_web.Datashare;

//a datastore is the container for our data.
//in this sample, i've only implemented a generic 'list data store'. it's our database.
//this 'hub' gives us all the different storage containers.

//this is separate from 'events', which we implement in the socket handler (SocketClient)


public static class DataStoreHub
{
	public static string[] AllStores => DataStores.Keys.ToArray();
	public static Action<string,int> OnConnectionChanged;
	public static Dictionary<string, int> Connections = new Dictionary<string, int>();
	public static Dictionary<string, IDataStore> DataStores = new Dictionary<string, IDataStore>();

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

	public static void CreateDataStore(string id, IDataStore store)
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