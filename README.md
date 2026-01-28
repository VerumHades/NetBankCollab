# NetBank
**NetBank** je P2P aplikace, která ukládá informace o účtech a umožňuje komunikaci mezi jednotlivými peery.

Aplikace je implementována v jazyce **C#**, je navržena jako **vícevrstvá (multi-tier) architektura** a uživatelské rozhraní je realizováno formou **React aplikace** s využitím komponentové knihovny **shadcn/ui**.

## Předpoklady
- mssql(nepoviná)
- sqlLite(nepoviná)
- .net 10
- nodeJs

## Instalace 
1. Klonujte repozitář:
```bash
git clone https://github.com/VerumHades/NetBankCollab.git
cd NetBankCollab
```

2. Klon repozitáře:
```bash
git clone https://github.com/VerumHades/NetBankCollab.git
cd NetBankCollab
```

3. Obnovení závislostí
```bash
dotnet restore
cd NetBank.Client
npm install 
```

4. Spusťte aplikaci:

```bash
dotnet run -- project NetBank.App
```
configuraci applikace muzete měnit iv cli za pomoci parametru které jsou v [link](https://github.com/VerumHades/NetBankCollab/blob/main/src/NetBank.Application/Configuration/Configuration.cs)


