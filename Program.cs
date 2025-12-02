//var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowItch",
//        policy =>
//        {
//            policy.SetIsOriginAllowed(origin =>
//            {
//                // Allow any subdomain of itch.io
//                return origin == "https://eickmayer.itch.io/" ||
//                       origin.EndsWith(".itch.io");
//            })
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//        });
//});

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseCors("AllowAll");

//app.UseAuthorization();

//app.MapControllers();

//app.Run();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware for itch.io WebGL
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault();

    if (!string.IsNullOrEmpty(origin) &&
        (origin == "https://eickmayer.itch.io" || origin.EndsWith(".itch.io")))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
        context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
    }

    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapControllers();

app.Run();
