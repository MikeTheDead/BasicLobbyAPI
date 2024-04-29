// using MongoDB.Driver;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Configuration;
//
// namespace LobbyAPI;
//
// public class RegisterServices
// {
//     private readonly IServiceCollection services;
//
//     public RegisterServices(IServiceCollection services, IConfiguration configuration)
//     {
//         var mongoDbSettings = configuration.GetSection("MongoDBSettings");
//
//         // Register MongoClient
//         services.AddSingleton<IMongoClient>(s =>
//         {
//             var connectionString = mongoDbSettings.GetValue<string>("ConnectionString");
//             return new MongoClient(connectionString);
//         });
//
//         // Register database and collections
//         services.AddSingleton<IMongoDatabase>(s =>
//         {
//             var client = s.GetRequiredService<IMongoClient>();
//             var databaseName = mongoDbSettings.GetValue<string>("Database");
//             return client.GetDatabase(databaseName);
//         });
//
//         // Dynamically create collections
//         services.AddSingleton(s =>
//         {
//             var database = s.GetRequiredService<IMongoDatabase>();
//             CreateCollections(database);
//             return database; // Example only, adjust based on actual needs
//         });
//         
//     }
//
//     private void CreateCollections(IMongoDatabase database)
//     {
//         services.AddSingleton(s =>
//         {
//             return database.GetCollection<dynamic>(Players);
//         });
//     }
// }