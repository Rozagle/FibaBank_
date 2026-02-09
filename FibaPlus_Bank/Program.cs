using FibaPlus_Bank.Models;
using FibaPlus_Bank.Services;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using FibaPlus_Bank.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<FibraPlusBankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<MarketDataService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SystemLogConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = Environment.GetEnvironmentVariable("RabbitMQConfig__HostName") ?? "localhost";
        var rabbitUser = Environment.GetEnvironmentVariable("RabbitMQConfig__UserName") ?? "guest";
        var rabbitPass = Environment.GetEnvironmentVariable("RabbitMQConfig__Password") ?? "guest";
        
        var rabbitPortStr = Environment.GetEnvironmentVariable("RabbitMQConfig__Port");
        int rabbitPort = string.IsNullOrEmpty(rabbitPortStr) ? 5672 : int.Parse(rabbitPortStr);

        // Bağlantıyı kur
        cfg.Host(rabbitHost, rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("system-log-queue", e =>
        {
            e.ConfigureConsumer<SystemLogConsumer>(context);
        });
    });
});
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
