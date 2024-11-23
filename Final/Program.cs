using Final;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;



Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(new ConfigurationBuilder()
		.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
		.Build())
	.CreateLogger();

try
{
	Log.Information("Запуск бота...");

	var builder = Host.CreateDefaultBuilder(args)
		.UseSerilog()
		.ConfigureServices((context, services) =>
		{
			var botApiKey = context.Configuration["Bot:ApiKey"];
			if (string.IsNullOrEmpty(botApiKey))
			{
				throw new Exception("Токен бота не задан в конфигурации!");
			}

			services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botApiKey));
			services.AddSingleton<IUserService, UserService>();
			services.AddHostedService<BotService>();
		});


	await builder.Build().RunAsync();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Программа завершилась с ошибкой");
}
finally
{
	Log.CloseAndFlush();
}

