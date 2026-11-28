using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Components.Tests;

public sealed class TextProviderTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Theory]
    [InlineData(FlText.Close, "Close")]
    [InlineData(FlText.Loading, "Loading")]
    public void RendersEnglishWithoutSetup(FlText text, string expected)
    {
        Assert.Equal(expected, RenderText(text));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HostProviderWinsInEitherRegistrationOrder(bool hostFirst)
    {
        if (hostFirst)
        {
            _context.Services.AddScoped<IFlTextProvider>(_ => new TestProvider("Fermer"));
        }

        _context.Services.AddFluentCmsComponents();

        if (!hostFirst)
        {
            _context.Services.AddScoped<IFlTextProvider>(_ => new TestProvider("Fermer"));
        }

        Assert.Equal("Fermer", RenderText(FlText.Close));
    }

    [Fact]
    public void FallsBackToEnglishWhenHostReturnsNull()
    {
        _context.Services.AddSingleton<IFlTextProvider>(new TestProvider("Fermer"));

        Assert.Equal("Loading", RenderText(FlText.Loading));
    }

    [Fact]
    public void FormatsNumberedArgumentsUsingCurrentUiCulture()
    {
        _context.Services.AddSingleton<IFlTextProvider>(new TestProvider("{1} : {0:F1}"));
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal("Total : 12,5", RenderText(FlText.Close, 12.5m, "Total"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void DefaultProviderIsSharedAcrossScopesAndSuppliesEveryEnglishText()
    {
        _context.Services.AddFluentCmsComponents();
        using IServiceScope scope = _context.Services.CreateScope();
        var english = _context.Services.GetRequiredService<IFlTextProvider>();

        Assert.Same(english, scope.ServiceProvider.GetRequiredService<IFlTextProvider>());
        Assert.All(Enum.GetValues<FlText>(), text => Assert.False(string.IsNullOrWhiteSpace(english.GetText(text))));
    }

    private string RenderText(FlText text, params object?[] arguments) =>
        _context.Render<TextComponent>(parameters => parameters
            .Add(component => component.Text, text)
            .Add(component => component.Arguments, arguments)).Markup;

    public void Dispose() => _context.Dispose();

    private sealed class TestProvider(string close) : IFlTextProvider
    {
        public string? GetText(FlText text) => text == FlText.Close ? close : null;
    }
}
