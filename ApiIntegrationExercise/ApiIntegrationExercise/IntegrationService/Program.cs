using IntegrationService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace IntegrationService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Customer & Order API Integration - Exercise             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Check if APIs are running
        if (!await CheckApisAsync())
        {
            Console.WriteLine("\n❌ Please start both APIs first:");
            Console.WriteLine("   Terminal 1: cd CustomerApi && dotnet run");
            Console.WriteLine("   Terminal 2: cd OrderApi && dotnet run");
            return;
        }
        
        Console.WriteLine("✅ Both APIs are running!\n");
        
        // Setup Dependency Injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();
        
        var orchestrator = serviceProvider.GetRequiredService<IntegrationOrchestrator>();
        var cts = new CancellationTokenSource();
        
        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) =>
        {
            Console.WriteLine("\n⚠️  Cancellation requested...");
            cts.Cancel();
            e.Cancel = true;
        };
        
        try
        {
            await RunIntegrationScenarios(orchestrator, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n✅ Cancellation handled gracefully!");
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine($"\n⚠️  Not yet implemented: {ex.Message}");
            Console.WriteLine("   Please implement the required methods in the Services folder.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Unexpected error: {ex.GetType().Name}");
            Console.WriteLine($"   {ex.Message}");
        }
        
        Console.WriteLine("\n" + new string('═', 64));
        Console.WriteLine("🎯 Exercise complete! Review your implementation.");
    }
    
    static void ConfigureServices(IServiceCollection services)
    {
        // Configure HttpClient for CustomerApi
        services.AddHttpClient<CustomerService>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5001");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        // TODO: Add Polly policies via .AddPolicyHandler()
        
        // Configure HttpClient for OrderApi
        services.AddHttpClient<OrderService>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5002");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        // TODO: Add Polly policies via .AddPolicyHandler()
        
        // Register orchestrator
        services.AddScoped<IntegrationOrchestrator>();
    }
    
    static async Task<bool> CheckApisAsync()
    {
        using var customerClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001") };
        using var orderClient = new HttpClient { BaseAddress = new Uri("http://localhost:5002") };
        
        Console.WriteLine("🔍 Checking if APIs are running...");
        
        try
        {
            var customerHealth = await customerClient.GetAsync("/health");
            if (!customerHealth.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ CustomerApi health check failed");
                return false;
            }
            Console.WriteLine("✓ CustomerApi is healthy (http://localhost:5001)");
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("❌ CustomerApi is not running (http://localhost:5001)");
            return false;
        }
        
        try
        {
            var orderHealth = await orderClient.GetAsync("/health");
            if (!orderHealth.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ OrderApi health check failed");
                return false;
            }
            Console.WriteLine("✓ OrderApi is healthy (http://localhost:5002)");
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("❌ OrderApi is not running (http://localhost:5002)");
            return false;
        }
        
        return true;
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
    
    static void PrintResult(string scenarioName, IntegrationService.Models.IntegrationResult result)
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
