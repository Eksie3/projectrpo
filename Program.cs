var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) { 
    app.MapOpenApi(); 
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();