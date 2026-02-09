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
    var hostAddress = Environment.GetEnvironmentVariable("RabbitMQConfig__HostName");

    if (string.IsNullOrEmpty(hostAddress))
    {
        hostAddress = "localhost";
    }

    cfg.Host(hostAddress, 5672, "/", h =>
    {
        h.Username("guest");
        h.Password("guest");
    });

            cfg.ReceiveEndpoint("system-log-queue", e =>
        {
            e.ConfigureConsumer<SystemLogConsumer>(context);
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
