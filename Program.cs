using Gerente.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Adicionar CORS para permitir requisições de arquivos locais
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFiles", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "AntiforgeryToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Adiciona suporte a sessão
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registrar o serviço de redefinição de senha
builder.Services.AddScoped<PasswordResetService>();

// Registrar o serviço de controle de acesso
builder.Services.AddScoped<AccessControlService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // Remover ou comentar o bloco HSTS se não for necessário
    // app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

// Adicionar CORS
app.UseCors("AllowLocalFiles");

// Adiciona o middleware de sessão
app.UseSession();

app.UseAuthorization();

// Middleware para redirecionar para login se não houver sessão ativa
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();
    if (!string.IsNullOrEmpty(path) && 
        !path.StartsWith("/login") && 
        !path.StartsWith("/css") && 
        !path.StartsWith("/js") && 
        !path.StartsWith("/lib") &&
        !path.StartsWith("/favicon") &&
        !context.Session.Keys.Contains("UserId"))
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
