using Microsoft.Extensions.Logging;
using CarnetDepenses.ViewModels;
using CarnetDepenses.Services;

namespace CarnetDepenses;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton(new HttpClient(new FakeApiHandler()));
		builder.Services.AddSingleton<IApiService, ApiService>();
		builder.Services.AddSingleton<IDepenseRepository, JsonDepenseRepository>();
		builder.Services.AddSingleton<DepensesViewModel>();
		builder.Services.AddSingleton<MainPage>();

		return builder.Build();
	}
}
