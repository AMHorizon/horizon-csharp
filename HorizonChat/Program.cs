using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using HorizonChat.Data;
using HorizonChat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<WebSocketHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseWebSockets();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// WebSocket endpoint
app.Map("/ws", async (HttpContext context, WebSocketHandler handler) =>
{
    await handler.HandleWebSocketAsync(context);
});

app.Run();
