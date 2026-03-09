using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Repositories;
using Domain.Interfaces.Repositories;
using Application.UseCases.Products;
using Application.UseCases.Orders;
using Application.UseCases.Customers;
using Application.UseCases.Purchases;
using Application.UseCases.Shipments;
using Integrations.SuperFrete.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PaintballManager",
    "paintballmanager.db");

var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<PaintballManagerDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Repositories
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<ISupplierRepository, EfSupplierRepository>();
builder.Services.AddScoped<IPurchaseRepository, EfPurchaseRepository>();
builder.Services.AddScoped<IKitRepository, EfKitRepository>();
builder.Services.AddScoped<IShipmentRepository, EfShipmentRepository>();

// Use Cases
builder.Services.AddTransient<CreateProductUseCase>();
builder.Services.AddTransient<CreateOrderUseCase>();
builder.Services.AddTransient<UpdateOrderStatusUseCase>();
builder.Services.AddTransient<RegisterCustomerUseCase>();
builder.Services.AddTransient<RegisterPurchaseUseCase>();
builder.Services.AddTransient<GenerateShipmentUseCase>();

// SuperFrete Integration
builder.Services.AddSuperFrete(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "PaintballManager API",
        Version = "v1",
        Description = "API para gerenciamento de loja de paintball - Produtos, Pedidos, Clientes, Frete"
    });
});

// CORS for website integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebsite", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaintballManagerDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaintballManager API v1");
    });
}

app.UseCors("AllowWebsite");
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
