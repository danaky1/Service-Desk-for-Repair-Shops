using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Core.Entities;
using ServiceDeskPro.Core.Interfaces;
using ServiceDeskPro.Core.Services;
using ServiceDeskPro.Infrastructure.Data;
using ServiceDeskPro.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ServiceDesk Pro API",
        Version = "v1",
        Description = "API для системы управления сервисным центром",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "ServiceDesk Pro Team",
            Email = "support@servicedesk-pro.ru"
        }
    });
    
    // Включаем XML комментарии (если есть)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ServiceDeskPro.API")));

// Add Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IMasterRepository, MasterRepository>();
builder.Services.AddScoped<ISparePartRepository, SparePartRepository>();

// Add Services
builder.Services.AddScoped<IOrderService, OrderService>();

// Add CORS - настроим для разработки и продакшена
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy",
        policy =>
        {
            // Добавьте localhost:3000 (Vite) и localhost:8080 (Vue CLI)
            policy.WithOrigins(
                    "http://localhost:3000", 
                    "https://localhost:3000",
                    "http://localhost:8080", 
                    "https://localhost:8080",
                    "http://127.0.0.1:3000",
                    "https://127.0.0.1:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });

    options.AddPolicy("ProductionPolicy",
        policy =>
        {
            policy.WithOrigins("https://servicedesk-pro.ru")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Add response compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServiceDesk Pro API v1");
        c.RoutePrefix = "swagger"; // Доступ по /swagger
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
    });
    
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts(); // HTTP Strict Transport Security
    app.UseCors("ProductionPolicy");
}

// Apply response compression
app.UseResponseCompression();

// В режиме разработки отключаем перенаправление на HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Error handling endpoint
app.Map("/error", (HttpContext context) =>
{
    var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
    var exception = exceptionHandlerPathFeature?.Error;
    
    var result = new
    {
        error = "Произошла ошибка на сервере",
        message = app.Environment.IsDevelopment() ? exception?.Message : null,
        path = exceptionHandlerPathFeature?.Path,
        timestamp = DateTime.UtcNow
    };
    
    return Results.Json(result, statusCode: 500);
});

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Проверяем подключение к базе данных
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new Exception("Не удалось подключиться к базе данных PostgreSQL. Проверьте строку подключения.");
        }
        
        // Применяем миграции
        await dbContext.Database.MigrateAsync();
        
        // Сидим начальные данные
        await SeedDataAsync(dbContext);
        
        Console.WriteLine("✅ База данных успешно настроена");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка настройки базы данных: {ex.Message}");
        throw;
    }
}

app.MapControllers();

app.Run();

// Метод для заполнения начальными данными
async Task SeedDataAsync(ApplicationDbContext context)
{
    // Сидим клиентов
    if (!await context.Clients.AnyAsync())
    {
        Console.WriteLine("📝 Заполняем таблицу клиентов...");
        
        var clients = new[]
        {
            new Client 
            { 
                Name = "Иван Иванов", 
                Phone = "+7 (999) 123-45-67", 
                Email = "ivan@mail.ru",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true
            },
            new Client 
            { 
                Name = "Мария Петрова", 
                Phone = "+7 (999) 987-65-43", 
                Email = "maria@mail.ru",
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                IsActive = true
            },
            new Client 
            { 
                Name = "Алексей Сидоров", 
                Phone = "+7 (999) 555-44-33", 
                Email = "alex@mail.ru",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                IsActive = true
            },
            new Client 
            { 
                Name = "Елена Козлова", 
                Phone = "+7 (999) 222-33-44", 
                Email = "elena@mail.ru",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                IsActive = true
            },
            new Client 
            { 
                Name = "Дмитрий Морозов", 
                Phone = "+7 (999) 777-88-99", 
                Email = "dmitry@mail.ru",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                IsActive = true
            }
        };
        
        await context.Clients.AddRangeAsync(clients);
        await context.SaveChangesAsync();
        Console.WriteLine($"✅ Добавлено {clients.Length} клиентов");
    }
    
    // Сидим мастеров
    if (!await context.Masters.AnyAsync())
    {
        Console.WriteLine("🔧 Заполняем таблицу мастеров...");
        
        var masters = new[]
        {
            new Master 
            { 
                Name = "Петр Васильев", 
                Specialization = "Смартфоны, планшеты, умные часы", 
                Email = "petr@servicedesk.ru",
                Phone = "+7 (999) 111-22-33",
                HourlyRate = 850,
                Rating = 4.8m,
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                IsActive = true
            },
            new Master 
            { 
                Name = "Сергей Козлов", 
                Specialization = "Ноутбуки, компьютеры, мониторы", 
                Email = "sergey@servicedesk.ru",
                Phone = "+7 (999) 222-33-44",
                HourlyRate = 950,
                Rating = 4.9m,
                CreatedAt = DateTime.UtcNow.AddDays(-85),
                IsActive = true
            },
            new Master 
            { 
                Name = "Анна Морозова", 
                Specialization = "Бытовые устройства, аудиотехника", 
                Email = "anna@servicedesk.ru",
                Phone = "+7 (999) 333-44-55",
                HourlyRate = 700,
                Rating = 4.7m,
                CreatedAt = DateTime.UtcNow.AddDays(-80),
                IsActive = true
            },
            new Master 
            { 
                Name = "Михаил Соколов", 
                Specialization = "Игровые консоли, VR-устройства", 
                Email = "mikhail@servicedesk.ru",
                Phone = "+7 (999) 444-55-66",
                HourlyRate = 900,
                Rating = 4.6m,
                CreatedAt = DateTime.UtcNow.AddDays(-75),
                IsActive = true
            }
        };
        
        await context.Masters.AddRangeAsync(masters);
        await context.SaveChangesAsync();
        Console.WriteLine($"✅ Добавлено {masters.Length} мастеров");
    }
    
    // Сидим запчасти
    if (!await context.SpareParts.AnyAsync())
    {
        Console.WriteLine("🔩 Заполняем таблицу запчастей...");
        
        var spareParts = new[]
        {
            new SparePart 
            { 
                Name = "Аккумулятор iPhone 13", 
                Sku = "BATT-IP13-001", 
                Manufacturer = "Apple",
                Description = "Оригинальный аккумулятор для iPhone 13, емкость 3227 mAh",
                Quantity = 15,
                Price = 3200,
                MinStockLevel = 5,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new SparePart 
            { 
                Name = "Экран Samsung Galaxy S21", 
                Sku = "DISP-SS21-001", 
                Manufacturer = "Samsung",
                Description = "OLED дисплей с сенсором для Samsung Galaxy S21",
                Quantity = 8,
                Price = 7500,
                MinStockLevel = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-55)
            },
            new SparePart 
            { 
                Name = "Клавиатура MacBook Air M1", 
                Sku = "KEY-MBA-M1-001", 
                Manufacturer = "Apple",
                Description = "Оригинальная клавиатура с подсветкой для MacBook Air M1",
                Quantity = 5,
                Price = 4500,
                MinStockLevel = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new SparePart 
            { 
                Name = "Камера iPhone 14 Pro", 
                Sku = "CAM-IP14P-001", 
                Manufacturer = "Apple",
                Description = "Основная камера 48MP для iPhone 14 Pro",
                Quantity = 12,
                Price = 8500,
                MinStockLevel = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            },
            new SparePart 
            { 
                Name = "Жесткий диск SSD 1TB", 
                Sku = "SSD-1TB-001", 
                Manufacturer = "Samsung",
                Description = "SSD диск NVMe PCIe 4.0 для ноутбуков",
                Quantity = 20,
                Price = 6500,
                MinStockLevel = 10,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            },
            new SparePart 
            { 
                Name = "Оперативная память 16GB DDR4", 
                Sku = "RAM-16GB-DDR4", 
                Manufacturer = "Kingston",
                Description = "Оперативная память 16GB DDR4 3200MHz",
                Quantity = 25,
                Price = 3800,
                MinStockLevel = 8,
                CreatedAt = DateTime.UtcNow.AddDays(-35)
            }
        };
        
        await context.SpareParts.AddRangeAsync(spareParts);
        await context.SaveChangesAsync();
        Console.WriteLine($"✅ Добавлено {spareParts.Length} запчастей");
    }
    
    // Сидим заказы
    if (!await context.Orders.AnyAsync())
    {
        Console.WriteLine("📋 Заполняем таблицу заказов...");
        
        var clients = await context.Clients.ToListAsync();
        var masters = await context.Masters.ToListAsync();
        
        if (clients.Any() && masters.Any())
        {
            var random = new Random();
            var statuses = new[] { "new", "diagnostics", "waiting_parts", "repair", "ready", "completed" };
            var devices = new[] 
            { 
                "iPhone 13 Pro", "Samsung Galaxy S22", "MacBook Pro M2", 
                "iPad Pro 12.9", "Sony PlayStation 5", "Dell XPS 13",
                "Asus ROG Zephyrus", "Google Pixel 7", "Xiaomi 13 Pro",
                "OnePlus 11", "HP Spectre x360", "Lenovo ThinkPad X1"
            };
            
            var orders = new List<Order>();
            
            for (int i = 0; i < 20; i++)
            {
                var client = clients[random.Next(clients.Count)];
                var master = masters[random.Next(masters.Count)];
                var status = statuses[random.Next(statuses.Length)];
                var device = devices[random.Next(devices.Length)];
                var isUrgent = random.Next(0, 10) < 3; // 30% срочных заказов
                
                var order = new Order
                {
                    OrderNumber = $"ORD-{DateTime.UtcNow.AddDays(-random.Next(1, 60)):yyyyMMdd}-{1000 + i}",
                    ClientId = client.Id,
                    DeviceName = device,
                    DeviceModel = GetDeviceModel(device),
                    SerialNumber = $"SN{DateTime.Now:yyyyMMdd}{random.Next(10000, 99999)}",
                    ProblemDescription = GetRandomProblem(device),
                    Status = status,
                    MasterId = master.Id,
                    TotalCost = random.Next(2000, 15000),
                    PartsCost = random.Next(1000, 8000),
                    LaborCost = random.Next(500, 4000),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 60)),
                    AcceptedAt = status != "new" ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null,
                    StartedAt = status == "repair" || status == "ready" || status == "completed" 
                        ? DateTime.UtcNow.AddDays(-random.Next(1, 20)) 
                        : null,
                    CompletedAt = status == "completed" 
                        ? DateTime.UtcNow.AddDays(-random.Next(1, 10)) 
                        : null,
                    EstimatedCompletionDate = status != "completed" 
                        ? DateTime.UtcNow.AddDays(random.Next(1, 14)) 
                        : null,
                    IsUrgent = isUrgent,
                    WarrantyPeriod = 90,
                    DiagnosticNotes = status != "new" 
                        ? $"Диагностика проведена: {GetRandomDiagnosis()}" 
                        : null
                };
                
                orders.Add(order);
            }
            
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Добавлено {orders.Count} заказов");
        }
        else
        {
            Console.WriteLine("⚠️ Не удалось создать заказы: отсутствуют клиенты или мастера");
        }
    }
    
    Console.WriteLine("✅ Начальные данные успешно добавлены в базу данных");
}

// Вспомогательные методы для генерации данных
string GetDeviceModel(string device)
{
    return device switch
    {
        "iPhone 13 Pro" => "A2483",
        "Samsung Galaxy S22" => "SM-S901",
        "MacBook Pro M2" => "A2338",
        "iPad Pro 12.9" => "A2436",
        "Sony PlayStation 5" => "CFI-1200",
        "Dell XPS 13" => "9310",
        "Asus ROG Zephyrus" => "G14",
        "Google Pixel 7" => "GVU6C",
        "Xiaomi 13 Pro" => "2210132C",
        "OnePlus 11" => "CPH2447",
        "HP Spectre x360" => "14-ea0023dx",
        "Lenovo ThinkPad X1" => "20U9001KRT",
        _ => "Модель не указана"
    };
}

string GetRandomProblem(string device)
{
    var problems = new[]
    {
        "Не включается",
        "Не заряжается",
        "Разбит экран",
        "Не работает динамик",
        "Проблемы с Wi-Fi",
        "Не работает камера",
        "Перегревается",
        "Не держит заряд батареи",
        "Не работает кнопка включения",
        "Проблемы с микрофоном",
        "Не работает сенсор",
        "Вода внутри устройства",
        "Не загружается операционная система",
        "Проблемы с Bluetooth"
    };
    
    var random = new Random();
    return $"{device}: {problems[random.Next(problems.Length)]}";
}

string GetRandomDiagnosis()
{
    var diagnoses = new[]
    {
        "Требуется замена экрана",
        "Неисправность батареи",
        "Проблема с материнской платой",
        "Требуется чистка от пыли",
        "Поврежден разъем зарядки",
        "Неисправность процессора",
        "Проблемы с оперативной памятью",
        "Требуется перепрошивка",
        "Повреждение от влаги",
        "Механическое повреждение корпуса"
    };
    
    var random = new Random();
    return diagnoses[random.Next(diagnoses.Length)];
}