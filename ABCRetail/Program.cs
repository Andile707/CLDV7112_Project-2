using ABCRetail.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<ITableStorageService,
    TableStorageService>();

builder.Services.AddSingleton<IBlobStorageService,
    BlobStorageService>();

builder.Services.AddSingleton<IQueueStorageService,
    QueueStorageService>();

builder.Services.AddSingleton<IFileStorageService,
    FileStorageService>();

builder.Services.AddHttpClient();

builder.Services.AddScoped<EventHubService>();
builder.Services.AddScoped<ServiceBusService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
