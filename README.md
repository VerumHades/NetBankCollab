# NetBank

**NetBank** is a P2P application that stores account information and enables communication between individual peers.

The application is implemented in **C#**, designed using a **multi-tier architecture**, and the user interface is built as a **React application** using the **shadcn/ui** component library.

## Prerequisites

-   MSSQL (optional)
-   SQLite (optional)
-   .NET 10
-   Node.js

## Installation

You can run the application either from **source code** or by using the **latest release package**.

### Option 1: Run from source

1.  Clone the repository:

```bash
git clone https://github.com/VerumHades/NetBankCollab.git
cd NetBankCollab
```

2.  Restore dependencies:

```bash
dotnet restore
cd NetBank.Client
npm install
```

3.  Run the application:

```bash
dotnet run --project NetBank.App
```

---

### Option 2: Run from the latest release

1.  Download the [latest release](https://github.com/VerumHades/NetBankCollab/releases/latest) from the GitHub releases page.
2.  Extract the archive. It contains:
    

-   `bin/` – prebuilt .NET application binaries
-   `Client/` – React client

3.  Start the backend application from the extracted directory:

```bash
./bin/NetBank.App
```

4.  Serve the client application from the `Client` directory:

```bash
cd Client
npm install
npm run dev
```

Application configuration can also be modified from the command line (CLI) using parameters defined in this file:  
[Configuration.cs](https://github.com/VerumHades/NetBankCollab/blob/main/src/NetBank.Application/Configuration/Configuration.cs)

The application will start the OpenAPI documentation at:  
[http://localhost:8444/swagger](http://localhost:8444/swagger)

## Command-line options

The application can be configured via command-line arguments. The following table lists all supported options, their defaults, and descriptions.

| Option                                | Short | JSON Key                         | Type | Default                 | Description                               |
|---------------------------------------|-------|----------------------------------|------|-------------------------|-------------------------------------------|
| `--config`                            | -     | -                                | string | -                       | Path of the config you want to load       |
| `--ip`                                | `-i`  | `serverIp`                       | string | `127.0.0.1`             | IP address for the orchestrator server    |
| `--port`                              | `-p`  | `serverPort`                     | int | `5000`                  | Port for the TCP orchestrator server      |
| `--delegation-target-port`            | —     | `delegationTargetPort`           | int | `5001`                  | Target port for the TCP command delegator |
| `--delegation-target-port-range-start` | —     | `delegationTargetPortRangeStart` | int | `5000`                  | Start of the delegation target port range |
| `--delegation-target-port-range-end`  | —     | `delegationTargetPortRangeEnd`   | int | `5010`                  | End of the delegation target port range   |
| `--timeout`                           | `-t`  | `networkInactivityTimeoutMs`     | int (ms) | `5000`                  | Network inactivity timeout in milliseconds |
| `--swap-delay`                        | `-d`  | `bufferSwapDelayMs`              | int (ms) | `50`                    | Delay before performing a buffer swap     |
| `--sql-lite-filename`                 | —     | `sqliteFilename`                 | string | `database.db`           | SQLite database filename                  |
| `--log-filepath`                      | —     | `logFilepath`                    | string | `log.txt`               | File path for the log output              |
| *(none)*                              | —     | `frontEndURl`                    | string | `http://localhost:8444` | Frontend application URL                  |

### Notes
- All numeric options must be **positive values**.
- `frontEndURl` is configurable via JSON only and has no CLI override.

### Example usage

```bash
dotnet NetBank.App.dll \
  --ip 0.0.0.0 \
  --port 8444 \
  --delegation-target-port-range-start 6000 \
  --delegation-target-port-range-end 6010 \
  --timeout 10000 \
  --sql-lite-filename prod.db \
  --log-filepath /var/log/netbank.log
```

## Protocol Commands

| Name | Code | Call | Success Response | Error Response |
|-----|-----|------|------------------|----------------|
| Bank code | `BC` | `BC` | `BC <IP>` | `ER <message>` |
| Account create | `AC` | `AC` | `AC <ACCOUNT>/<IP>` | `ER <message>` |
| Account deposit | `AD` | `AD <ACCOUNT>/<IP> <AMOUNT>` | `AD` | `ER <message>` |
| Account withdrawal | `AW` | `AW <ACCOUNT>/<IP> <AMOUNT>` | `AW` | `ER <message>` |
| Account balance | `AB` | `AB <ACCOUNT>/<IP>` | `AB <BALANCE>` | `ER <message>` |
| Account remove | `AR` | `AR <ACCOUNT>/<IP>` | `AR` | `ER <message>` |
| Bank total amount | `BA` | `BA` | `BA <AMOUNT>` | `ER <message>` |
| Bank number of clients | `BN` | `BN` | `BN <COUNT>` | `ER <message>` |
| Robbery plan | `RP` | `RP` | `RP <MESSAGE>` | `ER <message>` |

---

## Error Reference

This is a list of error messages you might receive from the server as a user.

| Error Message | Context & Resolution |
|--------------|----------------------|
| **Transaction failed.** Account `{Number}` has insufficient funds. Current balance: `{Balance}`, Attempted: `{Amount}`. | **Insufficient Funds:** You do not have enough money to cover this withdrawal. Reduce the amount or deposit more funds. |
| **The bank has reached its maximum account capacity.** No new accounts can be created. | **System Capacity:** We cannot open new accounts at this time due to system limits. |
| **The request was cancelled because the system buffer was cleared before processing finished.** | **System Reset:** A high-priority system update occurred while your request was in the queue. Please resubmit your transaction. |
| **Cannot close account.** A balance of `{Value}` remains. Please withdraw all funds first. | **Account Closure:** Accounts with a remaining balance cannot be closed. Withdraw all funds until the balance is exactly 0 to proceed. |
| **Access denied.** Account `{Number}` does not exist in our records. | **Account Not Found:** The account ID is incorrect or the account has been closed. Double-check the number and try again. |
| **An unspecified system error occurred.** | **General Error:** An unexpected technical issue occurred. |
