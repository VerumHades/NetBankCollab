namespace NetBank;

public record RobberyPlan(
    List<BankInfo> Banks,
    int TotalMoney,
    int TotalClients
);