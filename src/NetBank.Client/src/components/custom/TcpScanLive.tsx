import {useEffect, useRef, useState} from "react";
import {Button} from "@/components/ui/button";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card";
import {ScrollArea} from "@/components/ui/scroll-area";
import {Badge} from "@/components/ui/badge";
import {useAxiosClient} from "@/lib/api/axios-client.ts";

interface ScanProgress {
    Ip: string;
    Port: number;
    Status: string;
    Response?: string;
}

export default function TcpScanLive() {
    const [connected, setConnected] = useState(false);
    const [progress, setProgress] = useState<ScanProgress[]>([]);
    const axiosClient = useAxiosClient();
    const wsRef = useRef<WebSocket | null>(null);

    const openWebSocket = () => {
        // Close existing socket if any
        wsRef.current?.close();

        const ws = new WebSocket(import.meta.env.VITE_API_BASE_URL_SOCKET);
        wsRef.current = ws;

        ws.onopen = () => setConnected(true);
        ws.onclose = () => setConnected(false);

        ws.onmessage = (event) => {
            const data: ScanProgress = JSON.parse(event.data);
            setProgress(prev => [...prev, data]);
        };

        ws.onerror = (err) => {
            console.error("WebSocket error", err);
            ws.close();
        };
    };

    const startScan = async () => {
        setProgress([]);

        const body = {
            IpRangeStart: "192.168.0.1",
            IpRangeEnd: "192.168.0.10",
            Port: 80,
            TimeoutMs: 1000,
        };

        try {
            const res = await axiosClient.post(
                "/api/tcp-scan/start",
                body,
                { headers: { "Content-Type": "application/json" } }
            );

            console.log(res)
            if (res.status === 202) {
                openWebSocket(); // 👈 ONLY open socket after Accepted
            } else {
                console.error("Unexpected response status:", res.status);
            }
        } catch (err) {
            console.error("Failed to start scan", err);
        }
    };

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            wsRef.current?.close();
        };
    }, []);

    return (
        <Card className="shadow-md">
            <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="text-xl">TCP Scan – Live Progress</CardTitle>
                <Badge variant={connected ? "default" : "destructive"}>
                    {connected ? "Connected" : "Disconnected"}
                </Badge>
            </CardHeader>

            <CardContent className="space-y-4">
                <Button onClick={startScan}>
                    Start Scan
                </Button>

                <ScrollArea className="h-100 rounded-md border p-2">
                    <ul className="space-y-2 text-sm">
                        {progress.map((p, i) => (
                            <li
                                key={i}
                                className="flex flex-col rounded-lg bg-muted p-2"
                            >
                                <span className="font-mono">
                                    {p.Ip}:{p.Port}
                                </span>
                                <span className="text-xs text-muted-foreground">
                                    {p.Status}
                                    {p.Response && ` | ${p.Response}`}
                                </span>
                            </li>
                        ))}
                    </ul>
                </ScrollArea>
            </CardContent>
        </Card>
    );
}
