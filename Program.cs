var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Adiciona suporte a sessão
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Adiciona o middleware de sessão
app.UseSession();

app.UseAuthorization();

// Middleware para redirecionar para login se não houver sessão ativa
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();
    if (!string.IsNullOrEmpty(path) && !path.StartsWith("/login") && !path.StartsWith("/css") && !path.StartsWith("/js") && !context.Session.Keys.Contains("UserId"))
    {
        context.Response.Redirect("/Login");
        return;
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
