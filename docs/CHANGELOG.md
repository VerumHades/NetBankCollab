# Changelog

## [0.0.5] - 2026-01-28

### Added
- Account JSON serialization and conversion support (`AccountIdentifierConverter`, `AmountJsonConverter`). - **Michal Prihoda**
- TCP scanning frontend and backend integration (UI components, `TcpScanLive`, `IpRangeForm`, HTTP/WebSocket controllers). - **Michal Prihoda**
- Bank scanning and robbery services (`BankRobberyService`, `NetworkScanService`). - **Michal Prihoda**
- TODO.md completed tasks marked. - **Michal Prihoda**

### Updated
- README.md enhanced with installation steps, removed obsolete sections, and updated project details. - **Michal Prihoda**
- Parallelized TCP port discovery in `Program.cs` and `TcpConnectionPool.cs`. - **Filip Heger**
- Added text file logging and fixed buffer swap timer in `DoubleBufferedAccountService`. - **Filip Heger**
- HTTP default URL set in configuration. - **Michal Prihoda**

---

## [0.0.4] - 2026-01-27

### Added
- Command proxying with TCP delegator, pool, and end-to-end tests. - **Filip Heger**
- Swappable storage proxy for `AccountService`. - **Filip Heger**
- E2E tests for all commands completed. - **Filip Heger**
- CRUD functionality in AccountController. - **Michal Prihoda**
- Network scanning services and HTTP controllers. - **Michal Prihoda**
- WebSocket update support for scanning. - **Michal Prihoda**
- HTTP server startup logic. - **Michal Prihoda**

### Updated
- Services fixes in `Program.cs` and `HttpServerHost.cs`. - **Michal Prihoda**

---

## [0.0.3] - 2026-01-26

### Added
- End-to-end tests for command execution. - **Filip Heger**
- HTTP server and routing, including controllers and feature providers. - **Michal Prihoda**
- Initial frontend setup for NetBank client. - **Michal Prihoda**
- Swagger generation for HTTP API. - **Michal Prihoda**

### Updated
- Reworked errors and began integrating E2E tests. - **Filip Heger**
- Double buffering updates in `DoubleBufferedAccountService`. - **Filip Heger**
- Account action double buffering reworked. - **Filip Heger**

---

## [0.0.2] - 2026-01-25

### Added
- Simple SQLite storage implementation. - **Filip Heger**

### Updated
- Minor fixes in project files and references. - **Filip Heger**
- Storage reworked to work with entities. - **Filip Heger**
- TODO.md updated with new tasks. - **Michal Prihoda**

### Removed
- Git-tracked bin/obj folders. - **Filip Heger**

---

## [0.0.1] - 2026-01-23

### Added
- Initial solution structure and NetBank projects (`Application`, `Controllers`, `Infrastructure`). - **Filip Heger**
- Git ignore rules added. - **Filip Heger**
- Initial `NetBank.csproj` and unit tests project. - **Filip Heger**
- Initial commit of `NetBankCollab.sln` and basic project structure including domain, services, double-buffering architecture, error handling, and TCP command controllers (From personal P2P project). - **Filip Heger**
- Architecture diagrams (C1, C2, C3) and documentation (`CHANGELOG.md`, `TODO.md`). - **Filip Heger**
