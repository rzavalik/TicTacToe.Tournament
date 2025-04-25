using Microsoft.Playwright;
using Shouldly;

[Binding]
public class LayoutSteps
{
    private readonly ScenarioContext _context;
    private IPage _page = null!;
    private IBrowser _browser = null!;

    public LayoutSteps(ScenarioContext context)
    {
        _context = context;
    }

    [BeforeScenario]
    public async Task SetupAsync()
    {
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        _page = await _browser.NewPageAsync();
        _context["page"] = _page;
    }

    [AfterScenario]
    public async Task TearDownAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
    }

    [Given("the home page is opened")]
    public async Task GivenIOpenTheHomePage()
    {
        await _page.GotoAsync("https://tictactoe-webui.victoriousriver-bb51cd74.eastus2.azurecontainerapps.io/Home/Privacy");
    }

    [Then("the \"(.*)\" navigation button should be visible")]
    public async Task ThenTheNavbarButtonShouldBeVisible(string label)
    {
        var locator = label switch
        {
            "Home" => _page.Locator("nav a", new() { HasTextString = "Home" }),
            "Privacy" => _page.Locator("nav a", new() { HasTextString = "Privacy" }),
            _ => throw new ArgumentException($"Button {label} not found.")
        };

        var isVisible = await locator.IsVisibleAsync();

        isVisible.ShouldBeTrue();
    }
}