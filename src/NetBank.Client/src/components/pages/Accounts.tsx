
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
    createAccount,
    deleteAccount,
    deposit,
    withdraw,
    getBalance,
} from "@/lib/account.api";

export function AccountPanel() {
    const [accountId, setAccountId] = useState("");
    const [amount, setAmount] = useState("");
    const [balance, setBalance] = useState<number | null>(null);

    async function handleCreate() {
        const id = await createAccount();
        setAccountId(id);
        setBalance(0);
    }

    async function handleBalance() {
        const result = await getBalance(accountId);
        setBalance(result.value);
    }

    return (
        <Card className="max-w-md mx-auto">
            <CardHeader>
                <CardTitle>Bank Account</CardTitle>
            </CardHeader>

            <CardContent className="space-y-4">
                <Button className="w-full" onClick={handleCreate}>
                    Create Account
                </Button>

                <Input
                    placeholder="Account ID"
                    value={accountId}
                    onChange={(e) => setAccountId(e.target.value)}
                />

                <Input
                    type="number"
                    placeholder="Amount"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                />

                <div className="flex gap-2">
                    <Button
                        className="flex-1"
                        onClick={() => deposit(accountId, Number(amount))}
                    >
                        Deposit
                    </Button>

                    <Button
                        className="flex-1"
                        variant="secondary"
                        onClick={() => withdraw(accountId, Number(amount))}
                    >
                        Withdraw
                    </Button>
                </div>

                <Button variant="outline" className="w-full" onClick={handleBalance}>
                    Get Balance
                </Button>

                {balance !== null && (
                    <p className="text-center font-semibold">
                        Balance: {balance}
                    </p>
                )}

                <Button
                    variant="destructive"
                    className="w-full"
                    onClick={() => deleteAccount(accountId)}
                >
                    Delete Account
                </Button>
            </CardContent>
        </Card>
    );
}
