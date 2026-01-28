using Microsoft.Extensions.Logging;
using NetBank.NetworkScan;
using NetBank.Services.Implementations;
using NetBank.Services.NetworkScan;

namespace NetBank.Services;

public class BankRobberyService
{
    private readonly ILogger<BankRobberyService> _logger;
    private readonly NetworkScanService _networkScanService;

    public BankRobberyService(ILogger<BankRobberyService> logger, NetworkScanService  networkScanService)
    {
        _logger = logger;
        _networkScanService = networkScanService;
    }

    public async Task<string> Scan(int targetAmount)
    {
        _logger.LogInformation("Starting RobberyScan....");
        var banks = new List<BankInfo>();
        // load bank data 
        await _networkScanService.StartScanAsync(new ScanRequest(),CancellationToken.None);
        _logger.LogInformation("Loading bank data ");
        foreach (var scan in _networkScanService.Store.GetAll())
        {
            if (scan .Response != "found")
            {
               continue;
            }
            _logger.LogInformation("Loading bank {ip}:{port}", scan.Ip, scan.Port);
            var bank = await BankTcpClient.LoadBankInfoAsync(
                scan.Ip,
                scan.Port,
                TimeSpan.FromSeconds(2));

            if (bank == null)
            {
                _logger.LogWarning(
                    "Failed to load data from bank {ip}:{port}",
                    scan.Ip, scan.Port);
                continue;
            }
            banks.Add(bank);
        }
        if (!banks.Any())
        {
            _logger.LogWarning("No banks available for robbery planning");
            return "No banks available.";
        }
        var plan = Plan(banks, targetAmount);

        // 4️⃣ format RP response
        return FormatRpMessage(targetAmount, plan);
    }

    public static RobberyPlan Plan(
        IEnumerable<BankInfo> banks,
        int targetAmount)
    {
        var orderedBanks = banks
            .OrderByDescending(b => (double)b.Money / b.Clients)
            .ToList();

        var selected = new List<BankInfo>();
        int totalMoney = 0;
        int totalClients = 0;

        foreach (var bank in orderedBanks)
        {
            if (totalMoney >= targetAmount)
                break;

            selected.Add(bank);
            totalMoney += bank.Money;
            totalClients += bank.Clients;
        }

        return new RobberyPlan(
            selected,
            totalMoney,
            totalClients
        );
    }
    
    public static string FormatRpMessage(int target, RobberyPlan plan)
    {
        var bankList = string.Join(
            " a ",
            plan.Banks.Select(b => b.Ip)
        );

        return
            $"RP K dosažení {target} je třeba vyloupit banky {bankList}. " +
            $"Získáno {plan.TotalMoney} RP a poškozeno bude pouze {plan.TotalClients} klientů.";
    }

    
}