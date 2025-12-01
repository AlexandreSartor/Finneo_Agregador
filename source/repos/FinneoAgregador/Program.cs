using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Adiciona controllers
builder.Services.AddControllers();



// Registra HttpClient singleton
builder.Services.AddSingleton<HttpClient>();

// Registra CryptoService como singleton e injeta HttpClient
builder.Services.AddSingleton<CryptoService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    return new CryptoService(httpClient);
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
