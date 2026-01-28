using System.Net.Sockets;
using System.Text;

namespace NetBank;

public class BankTcpClient
{
    public static async Task<BankInfo?> LoadBankInfoAsync(
        string ip,
        int port,
        TimeSpan timeout)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(timeout);

            await client.ConnectAsync(ip, port, cts.Token);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII)
            {
                AutoFlush = true
            };
            using var reader = new StreamReader(stream, Encoding.ASCII);

            // ---- BA ----
            await writer.WriteLineAsync("BA");
            var ba = await reader.ReadLineAsync(cts.Token);

            if (ba == null || !ba.StartsWith("BA "))
                return null;

            int money = int.Parse(ba.Split(' ')[1]);

            // ---- BN ----
            await writer.WriteLineAsync("BN");
            var bn = await reader.ReadLineAsync(cts.Token);

            if (bn == null || !bn.StartsWith("BN "))
                return null;

            int clients = int.Parse(bn.Split(' ')[1]);

            return new BankInfo(ip, money, clients);
        }
        catch
        {
            // timeout / connection error / protocol error
            return null;
        }
    }
}