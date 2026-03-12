using Microsoft.EntityFrameworkCore;
using Npgsql;
using TTRPGHelper.Api.Common.RAG;
using TTRPGHelper.Api.Infrastructure.AI.LLM;
using TTRPGHelper.Api.Infrastructure.Database;
using TTRPGHelper.Api.Modules.Conversations;
using TTRPGHelper.Api.Modules.Conversations.Endpoints;
using TTRPGHelper.Api.Modules.KnowledgeBase;
using TTRPGHelper.Api.Modules.KnowledgeBase.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Database
var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.EnableDynamicJson();
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, o => o.UseVector()));

// AI Services
builder.Services.AddLlmServices(builder.Configuration);

// RAG Services
builder.Services.AddSingleton<ChunkingService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<RetrievalService>();

// Module Services
builder.Services.AddScoped<IngestionService>();

// SignalR
builder.Services.AddSignalR();

// CORS for frontend dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

// Map module endpoints
app.MapWorldEntityEndpoints();
app.MapImportEndpoints();
app.MapRelationshipEndpoints();
app.MapConversationEndpoints();
app.MapProposalEndpoints();

// Map SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");

// Auto-migrate in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
