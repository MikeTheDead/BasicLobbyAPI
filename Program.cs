using LobbyAPI.Interfaces;
using LobbyAPI.Repositories;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

var builder = WebApplication.CreateBuilder(args);

#region Raven configuration

// IConfiguration in builder.Configuration
var ravenDbSettings = builder.Configuration.GetSection("RavenDBSettings");

IDocumentStore lobbyStore = new DocumentStore
{
    Urls = ravenDbSettings.GetSection("Urls").Get<string[]>(),
    Database = ravenDbSettings["Database"], 
    Conventions = { } 
};

lobbyStore.Initialize();
await EnsureDatabaseExists(lobbyStore, ravenDbSettings["Database"]);

IDocumentStore passwordStore = new DocumentStore
{
    Urls = ravenDbSettings.GetSection("Urls").Get<string[]>(),
    Database = "password", 
    Conventions = { } 
};

passwordStore.Initialize();
await EnsureDatabaseExists(passwordStore, "password");

//PKVP=player key value pair
IDocumentStore PKVPStore = new DocumentStore
{
    Urls = ravenDbSettings.GetSection("Urls").Get<string[]>(),
    Database = "playerkvp", 
    Conventions = { } 
};

PKVPStore.Initialize();
await EnsureDatabaseExists(PKVPStore, "playerkvp");



async Task EnsureDatabaseExists(IDocumentStore store, string databaseName, bool createDatabaseIfNotExists = true)
{
    try
    {
        await store.Maintenance.ForDatabase(databaseName).SendAsync(new GetStatisticsOperation());
    }
    catch (DatabaseDoesNotExistException)
    {
        if (createDatabaseIfNotExists)
        {
            await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(databaseName)));
        }
    }
}

#endregion

#region Repositories

IPasswordRepository pwdRepo = new PasswordRepository(passwordStore);
IPlayerRepository playerKVPRepo = new PlayerRepository(PKVPStore);
builder.Services.AddSingleton<ILobbyRepository>(new LobbyRepository(lobbyStore,pwdRepo,playerKVPRepo));
builder.Services.AddSingleton<IPasswordRepository>(pwdRepo);
builder.Services.AddSingleton<IPlayerRepository>(playerKVPRepo);

#endregion

// Add services to the container.
builder.Services.AddSingleton<IDocumentStore>(lobbyStore);
builder.Services.AddSingleton<IDocumentStore>(passwordStore);
builder.Services.AddSingleton<IDocumentStore>(PKVPStore);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();