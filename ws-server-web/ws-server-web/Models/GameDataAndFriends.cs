using System.Text.Json;
using System.Text.Json.Serialization;

namespace ws_server_web;

// A single datastore of some type.
public class GameData
{
	public Action OnQuestionUpdated;
	public string CurrentQuestion;
	
	public void UpdateQuestion(string newQuestion)
	{
		CurrentQuestion = newQuestion;
		OnQuestionUpdated?.Invoke();
	}
}