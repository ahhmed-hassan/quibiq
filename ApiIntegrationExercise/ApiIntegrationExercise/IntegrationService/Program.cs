using IntegrationService.Models;
using IntegrationService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     API Integration Exercise - Resilience Patterns         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        // Step 1: Build Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Step 2: Check if APIs are running
        Console.WriteLine("⏳ Checking if APIs are available...");

        if (!await CheckApiAvailability("http://localhost:5001/health", "CustomerApi"))
        {
            Console.WriteLine("❌ CustomerApi is not running. Please start it first.");
            return;
        }

        if (!await CheckApiAvailability("http://localhost:5002/health", "OrderApi"))
        {
            Console.WriteLine("❌ OrderApi is not running. Please start it first.");
            return;
        }

        Console.WriteLine("✅ Both APIs are running!\n");

        // Step 3: Setup Dependency Injection with Configuration
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Step 4: Get logger to test it works
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🚀 Integration Service started");

        var orchestrator = serviceProvider.GetRequiredService<IntegrationOrchestrator>();
        var cts = new CancellationTokenSource();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) =>
        {
            logger.LogWarning("⚠️  Cancellation requested...");
            cts.Cancel();
            e.Cancel = true;
        };

        try
        {
            await RunIntegrationScenarios(orchestrator, cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("✅ Cancellation handled gracefully!");
        }
        catch (NotImplementedException ex)
        {
            logger.LogError("⚠️  Not yet implemented: {Message}", ex.Message);
            Console.WriteLine("   Please implement the required methods in the Services folder.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Unexpected error: {ErrorType}", ex.GetType().Name);
        }

        Console.WriteLine("\n" + new string('═', 64));
        Console.WriteLine("🎯 Exercise complete! Review your implementation.");
    }

    static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register Configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Configure Logging from appsettings.json
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Get API URLs from configuration
        var customerApiUrl = configuration["ApiSettings:CustomerApiUrl"]
                             ?? "http://localhost:5001";
        var orderApiUrl = configuration["ApiSettings:OrderApiUrl"]
                          ?? "http://localhost:5002";
        var defaultTimeout = int.Parse(configuration["ApiSettings:DefaultTimeout"] ?? "10");

        // Configure HttpClient for CustomerApi
        services.AddHttpClient<CustomerService>(client =>
        {
            client.BaseAddress = new Uri(customerApiUrl);
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Configure HttpClient for OrderApi
        services.AddHttpClient<OrderService>(client =>
        {
            client.BaseAddress = new Uri(orderApiUrl);
            client.Timeout = TimeSpan.FromSeconds(defaultTimeout);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Register services
        services.AddScoped<IntegrationOrchestrator>();
    }

    static async Task<bool> CheckApiAvailability(string healthEndpoint, string apiName)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            var response = await client.GetAsync(healthEndpoint);
            Console.WriteLine($"✓ {apiName} is available at {healthEndpoint}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    static async Task RunIntegrationScenarios(IntegrationOrchestrator orchestrator, CancellationToken ct)
    {
        Console.WriteLine("Select a scenario to run:\n");
        Console.WriteLine("1. Process Pending Orders (Validate & Update Status)");
        Console.WriteLine("2. Complete Orders (Update Customer Balance & Loyalty)");
        Console.WriteLine("3. Sync Customer Loyalty (Reconcile All Data)");
        Console.WriteLine("4. Run All Scenarios Sequentially");
        Console.WriteLine("5. Exit");
        Console.WriteLine();
        Console.Write("Enter choice (1-5): ");

        var choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                await RunScenario1(orchestrator, ct);
                break;
            case "2":
                await RunScenario2(orchestrator, ct);
                break;
            case "3":
                await RunScenario3(orchestrator, ct);
                break;
            case "4":
                await RunScenario1(orchestrator, ct);
                Console.WriteLine("\n" + new string('═', 64) + "\n");
                await RunScenario2(orchestrator, ct);
                Console.WriteLine("\n" + new string('═', 64) + "\n");
                await RunScenario3(orchestrator, ct);
                break;
            case "5":
                Console.WriteLine("Exiting...");
                return;
            default:
                Console.WriteLine("Invalid choice. Please run again and select 1-5.");
                return;
        }

        Console.WriteLine("\n" + new string('─', 64));
        Console.WriteLine("\n💡 Tips for QUIBIQ Interview:");
        Console.WriteLine("   • Explain your retry strategy choices");
        Console.WriteLine("   • Discuss how you handle rate limits");
        Console.WriteLine("   • Describe error handling approach");
        Console.WriteLine("   • Talk about trade-offs (performance vs reliability)");
    }

    static async Task RunScenario1(IntegrationOrchestrator orchestrator, CancellationToken ct)
    {
        var result = await orchestrator.ProcessPendingOrdersAsync(ct);
        PrintResult("Scenario 1: Process Pending Orders", result);
    }

    static async Task RunScenario2(IntegrationOrchestrator orchestrator, CancellationToken ct)
    {
        var result = await orchestrator.CompleteOrdersAsync(ct);
        PrintResult("Scenario 2: Complete Orders", result);
    }

    static async Task RunScenario3(IntegrationOrchestrator orchestrator, CancellationToken ct)
    {
        var result = await orchestrator.SyncCustomerLoyaltyAsync(ct);
        PrintResult("Scenario 3: Sync Customer Loyalty", result);
    }

    static void PrintResult(string scenarioName, IntegrationResult result)
    {
        Console.WriteLine($"\n📊 {scenarioName} - Results:");
        Console.WriteLine(new string('─', 64));
        Console.WriteLine($"Total Processed:    {result.TotalOrdersProcessed}");
        Console.WriteLine($"✓ Successful:       {result.SuccessfulOrders}");
        Console.WriteLine($"✗ Failed:           {result.FailedOrders}");
        Console.WriteLine($"⏱  Duration:         {result.Duration.TotalSeconds:F2}s");

        if (result.FailedOrderIds.Any())
        {
            Console.WriteLine($"\nFailed Order IDs: {string.Join(", ", result.FailedOrderIds)}");
        }

        if (result.ErrorMessages.Any())
        {
            Console.WriteLine("\nError Messages:");
            foreach (var error in result.ErrorMessages.Take(5))
            {
                Console.WriteLine($"  • {error}");
            }
            if (result.ErrorMessages.Count > 5)
                Console.WriteLine($"  ... and {result.ErrorMessages.Count - 5} more");
        }
    }
}
