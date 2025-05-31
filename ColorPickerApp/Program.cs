using ColorPickerApp.Services; // Убедитесь, что это пространство имён указано

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку контроллеров с представлениями
builder.Services.AddControllersWithViews();

// Регистрируем сервисы
builder.Services.AddScoped<ExportService>(); // Добавьте эту строку
builder.Services.AddScoped<ImportService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles(); // Поддержка CSS, JS, картинок
app.UseRouting();

app.UseAuthorization();

// Регистрируем маршруты на верхнем уровне
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Color}/{action=Index}/{id?}"); // Открываем страницу /Color по умолчанию

app.Run();