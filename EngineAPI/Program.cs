using EngineAPI.Domain.Data;
using EngineAPI.Repository;
using EngineAPI.Service.Implementation;
using EngineAPI.Service.Implementationl;
using EngineAPI.Service.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using VDS.RDF.Query.Algebra;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connString = builder.Configuration.GetConnectionString("DBConn");
builder.Services.AddDbContext<IndexDBContext>(conn => conn.UseSqlServer(connString));

builder.Services.AddScoped(typeof(IRepository), typeof(Repository));
builder.Services.AddScoped<IConceptService, ConceptService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IQueryService, QueryService>();
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
