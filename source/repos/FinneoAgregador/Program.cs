using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ?? Adiciona controllers
builder.Services.AddControllers();

// ?? Adiciona Swagger (opcional, mas útil para teste)
builder.Services.AddEndpointsApiExplorer();


// ?? Registra CryptoService com HttpClient injetado
builder.Services.AddHttpClient<CryptoService>();

var app = builder.Build();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
