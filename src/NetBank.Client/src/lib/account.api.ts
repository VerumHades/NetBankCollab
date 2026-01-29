// lib/account.api.ts


import {useAxiosClient} from "@/lib/api/axios-client.ts";

export async function createAccount() {
    const axios = useAxiosClient();
    const { data } = await axios.post("/accounts");
    return data; // AccountIdentifier
}

export async function deleteAccount(accountId: string) {
    const axios = useAxiosClient();
    await axios.delete(`/accounts/${accountId}`);
}

export async function deposit(accountId: string, amount: number) {
    const axios = useAxiosClient();
    await axios.post(`/accounts/${accountId}/deposit`, {
        value: amount,
    });
}

export async function withdraw(accountId: string, amount: number) {
    const axios = useAxiosClient();
    await axios.post(`/accounts/${accountId}/withdraw`, {
        value: amount,
    });
}

export async function getBalance(accountId: string) {
    const axios = useAxiosClient();
    const { data } = await axios.get(`/accounts/${accountId}/balance`);
    return data; // { value: number }
}
