using FirstWebApi.Database;
using FirstWebApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

var connectionString = Environment.GetEnvironmentVariable("DefaultConnectionString")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("account").MapIdentityApi<IdentityUser>();

app.MapGet("/search", async (ApplicationDbContext db, [AsParameters] SearchModel model) =>
{
    var searchResult = await db.TestDatas
        .Where(d => 
            string.IsNullOrWhiteSpace(model.Name)
            || d.Name.ToLower().Contains(model.Name.ToLower())).ToListAsync();

    return searchResult;
});

app.MapGet("/get/{id}", async (ApplicationDbContext db, int id) =>
{
    var testData = await db.TestDatas.SingleOrDefaultAsync(d => d.Id == id);

    if (testData is null)
        return new TestDataViewModel { Error = "Nincs adat" };

    return new TestDataViewModel
    { 
        Id = testData.Id,
        Description = testData.Description,
        Name = testData.Name
    };
});

app.MapPost("/create", async (ApplicationDbContext db, TestDataCreateModel model) =>
{
    var newTestData = new TestData
    {
        Description = model.Description,
        Name = model.Name,
    };

    db.TestDatas.Add(newTestData);
    await db.SaveChangesAsync();
});

app.MapPut("/update", async (ApplicationDbContext db, TestDataUpdateModel model) =>
{
    var testData = await db.TestDatas.SingleOrDefaultAsync(d => d.Id == model.Id);
    if (testData is null)
        return Results.NotFound();

    testData.Name = model.Name;
    testData.Description = model.Description;

    await db.SaveChangesAsync();

    return Results.Ok();
});

app.MapDelete("/delete/{id}", async (ApplicationDbContext db, int id) =>
{
    var testData = await db.TestDatas.SingleOrDefaultAsync(d => d.Id == id);

    if (testData is null)
        return Results.NotFound();

    db.TestDatas.Remove(testData);
    await db.SaveChangesAsync();

    return Results.Ok();
});

app.Run();