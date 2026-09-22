using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ws_server_web.Datashare;

public class ListDataStore<T> : IDataStore where T : IList
{
	//Every item in the list gets a unique ID. It's not the index, it's independent of removal/operations.
	public Action<uint,T,string> OnItemAdded;
	public Action<uint, string> OnItemRemoved;
	public Action<uint, T, string> OnItemChanged;
	public Action OnAllClientsUpdateAll;
	public Action<string> OnClear;
	private readonly List<(uint id, T item)> _data = new List<(uint,T)>();
	private uint _nextID = 1;//We use 0 for "waiting for unique id".
	public uint AddItem(T item, string client)
	{
		var id = _nextID;
		_data.Add((_nextID,item));//add an item and give it an id.
		_nextID++;//then, next time we add, the id will still be unique.
		OnItemAdded?.Invoke(id,item,client);
		return id;
	}

	public void ChangeItem(uint id, T item, string client)
	{
		var i = _data.FindIndex(x => x.Item1 == id);
		if (i != -1)
		{
			if (_data[i].item.GetHashCode() == item.GetHashCode())
			{
				return;
			}
			//replace old with new value
			_data[i] = (id,item);
		}
		else
		{
			_data.Add((id,item));
		}
		OnItemChanged?.Invoke(id, item, client);
	}
	
	public void RemoveItem(uint id, string client)
	{
		var item = _data.Find(x => x.Item1 == id);
		_data.Remove(item);
		OnItemRemoved?.Invoke(id,client);
	}

	public void Clear(string client)
	{
		_data.Clear();
		OnClear?.Invoke(client);
	}

	public List<(uint, T)> GetAllRawData()
	{
		return _data;
	}


	public string Serialize()
	{
		var js = new JsonContainer();
		js.store = new List<JsonPair>();
		foreach (var tup in _data)
		{
			js.store.Add(new JsonPair(){id = tup.id,data = tup.item});
		}
		return JsonSerializer.Serialize<JsonContainer>(js);
	}

	public bool Deserialize(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return false;
		}
		
		var result = JsonSerializer.Deserialize<JsonContainer>(data);
		if (result == null)
		{
			return false;
		}

		if (result.store == null)
		{
			return false;
		}

		if (_data.Count > 0)
		{
			Clear("server");
		}
		foreach (var pair in result.store)
		{ 
			_data.Add((pair.id, pair.data));
		}
		OnAllClientsUpdateAll?.Invoke();
		return true;
	}

	[System.Serializable]
	public class JsonContainer
	{
		public List<JsonPair>? store { get; set; }
	}

	[System.Serializable]
	public class JsonPair
	{
		public uint id { get; set; }
		public T data { get; set; }
	}
}