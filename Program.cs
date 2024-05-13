using System.Diagnostics;
using LobbyAPI;
using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers;
using LobbyAPI.MongoCollectionControllers.Interface;
using LobbyAPI.Repositories;
using LobbyAPI.SignalRHubs;
using LobbyAPI.Utilities;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

#region MongoDB

//mongo settings
var mongoDbSettings = builder.Configuration.GetSection("MongoDBSettings");
var mongoClient = new MongoClient(mongoDbSettings.GetValue<string>("ConnectionString"));
var database = mongoClient.GetDatabase(mongoDbSettings.GetValue<string>("Database"));


builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(database);

//mongo collections (tried dynamically registering with assembly selection, but it was too unreliable)
builder.Services.AddSingleton(database.GetCollection<Player>("Players"));
builder.Services.AddSingleton(database.GetCollection<PlayerKey>("PlayerKeys"));
builder.Services.AddSingleton(database.GetCollection<Lobby>("Lobbies"));
builder.Services.AddSingleton(database.GetCollection<Password>("Passwords"));
builder.Services.AddSingleton(database.GetCollection<Session>("Sessions"));
builder.Services.AddSingleton(database.GetCollection<ConnectionAddress>("ConnAdds"));

//mongo controllers 
builder.Services.AddSingleton<IMongoController<Player>,PlayerMongoController>();
builder.Services.AddSingleton<IMongoController<PlayerKey>,PlayerKeyMongoController>();
builder.Services.AddSingleton<IMongoController<Lobby>,LobbyMongoController>();
builder.Services.AddSingleton<IMongoController<Password>,PasswordMongoController>();
builder.Services.AddSingleton<IMongoSessionExtension,SessionMongoController>();
builder.Services.AddSingleton<IMongoController<ConnectionAddress>,ConnectionAddressMongoController>();


#endregion

#region Repositories

//scuffed repository registration
builder.Services.AddSingleton<IPasswordRepository, PasswordRepository>();

SessionRepository session = null;
builder.Services.AddSingleton<IPlayerRepository>(s =>
{
    var player = s.GetRequiredService<IMongoController<Player>>();
    IMongoController<PlayerKey> key = s.GetRequiredService<IMongoController<PlayerKey>>();

    return new PlayerRepository(player,key);
});
builder.Services.AddSingleton<ISessionRepository>(s =>
{
    var sessionContoller = s.GetRequiredService<IMongoSessionExtension>();
    session = new SessionRepository(sessionContoller);
    return session;
});


builder.Services.AddSingleton<IConnectionAddressRepository>(s =>
{
    var pkvpStore = s.GetRequiredService<IMongoController<ConnectionAddress>>();
    return new ConnectionAddressRepository(pkvpStore);
});
builder.Services.AddSingleton<ILobbyRepository>(s =>
{
    var lobbyStore = s.GetRequiredService<IMongoController<Lobby>>();
    var pwdRepo = s.GetRequiredService<IPasswordRepository>();
    return new LobbyRepository(lobbyStore, pwdRepo);
});



#endregion

#region SignalR
//still need to setup heartbeats
builder.Services.AddSingleton<HubHandlerService>();
builder.Services.AddSingleton<HeartbeatHub>();

builder.Services.AddSingleton<ConnectionHub>(s =>
{
    var hubHandler = s.GetRequiredService<HubHandlerService>();
    var sessionRepo = s.GetRequiredService<ISessionRepository>();
    return new ConnectionHub(hubHandler, sessionRepo);
});

builder.Services.AddSingleton<SignalHubs>(s =>
{
    var connectionHub = s.GetRequiredService<ConnectionHub>();
    var heartbeatHub = s.GetRequiredService<HeartbeatHub>();
    return new SignalHubs(connectionHub, heartbeatHub);
});
builder.Services.AddSingleton<ISessionRepository, SessionRepository>();

builder.Services.AddSignalR();

#endregion

builder.Services.AddControllers();
//signalR
builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




var app = builder.Build();
app.Lifetime.ApplicationStopping.Register(() =>
{
    var collection = app.Services.GetRequiredService<IMongoCollection<Session>>();
    var lobbyCollection = app.Services.GetRequiredService<IMongoCollection<Lobby>>();
    DeleteAllSessions<Session>(collection).Wait();
    DeleteAllSessions<Lobby>(lobbyCollection).Wait();
});
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseEndpoints(ep =>
{
    ep.MapHub<ConnectionHub>("/connect");
});

app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();

app.Run();
static async Task DeleteAllSessions<T>(IMongoCollection<T> sessionCollection)
{
    await sessionCollection.DeleteManyAsync(FilterDefinition<T>.Empty);
}