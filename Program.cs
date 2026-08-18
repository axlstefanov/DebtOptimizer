using DebtOptimizer.Data;
using DebtOptimizer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddScoped<PaymentPlanService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddHttpClient<IDebtExtractor, GeminiDebtExtractor>(c =>
    c.Timeout = TimeSpan.FromSeconds(30));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();