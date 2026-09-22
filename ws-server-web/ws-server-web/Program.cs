using System.Net;
using ws_server_web;
using ws_server_web.Components;
using ws_server_web.Datashare;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

string[] endpoints = new[] { "test", "ws", "a", "b", "c" };


//configure the app to use websockets.
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};
app.UseWebSockets();

//configure the server endpoint. We are creating multiple for test convenience.
//Create a datastore and an endpoint for our list.
foreach (string storeID in endpoints)
{
    DataStoreHub.CreateDataStore(storeID, new GameData());

    app.Map(storeID, async context =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            SocketClient controller = new SocketClient(webSocket, storeID);
            await controller.Handle();

        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    });

}




// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
