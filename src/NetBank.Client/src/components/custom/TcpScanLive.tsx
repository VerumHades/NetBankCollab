import {useEffect, useRef, useState} from "react";
import {toast} from "sonner";
import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card";
import {ScrollArea} from "@/components/ui/scroll-area";
import {Badge} from "@/components/ui/badge";
import {useAxiosClient} from "@/lib/api/axios-client.ts";
import {IpRangeForm} from "@/components/from/IpRangeForm.tsx";

interface ScanProgress {
    Ip: string;
    Port: number;
    Status: string;
    Response?: string;
}

interface ScanEvent {
    Type: "progress" | "completed" | "cancelled";
    Payload?: any;
}

interface TcpScanRowProps {
    ip: string;
    port: number;
    status: string;
    response?: string;
}

const statusStyles: Record<string, string> = {
    scanning: "bg-grey-500/10 text-yellow-600 border-grey-500/30",
    timeout: "bg-yellow-500/10 text-yellow-600 border-yellow-500/30",
    found: "bg-green-500/10 text-green-600 border-green-500/30",
    error: "bg-red-500/10 text-red-600 border-red-500/30",
};


export default function TcpScanLive() {
    const [connected, setConnected] = useState(false);
    const [progress, setProgress] = useState<ScanProgress[]>([]);
    const axiosClient = useAxiosClient();
    const wsRef = useRef<WebSocket | null>(null);

    const openWebSocket = () => {
        wsRef.current?.close();

        const ws = new WebSocket(import.meta.env.VITE_API_BASE_URL_SOCKET);
        wsRef.current = ws;

        ws.onopen = () => setConnected(true);
        ws.onclose = () => setConnected(false);

        ws.onmessage = (event) => {
            const msg: ScanEvent = JSON.parse(event.data);

            switch (msg.Type) {
                case "progress":
                    setProgress((prev) => {
                        const idx = prev.findIndex(
                            p => p.Ip === msg.Payload.Ip && p.Port === msg.Payload.Port
                        );
                        let copy;
                        if (idx === -1) {
                            copy = [...prev, msg.Payload];
                        } else {
                            copy = [...prev];
                            copy[idx] = msg.Payload;
                        }
                        // Sort so successful scans (Status "Found") are on top
                        copy.sort((a, b) => {
                            const aOk = a.Status.toLowerCase() === "found";
                            const bOk = b.Status.toLowerCase() === "found";
                            return Number(bOk) - Number(aOk);
                        });
                        return copy;
                    });
                    break;

                case "completed":
                    toast.success("Network scan completed");
                    break;

                case "cancelled":
                    toast.warning("Network scan cancelled");
                    break;
            }
        };

        ws.onerror = (err) => {
            console.error("WebSocket error", err);
            ws.close();
        };
    };

    const startScan = async (body: any) => {
        setProgress([]);

   
        try {
            const res = await axiosClient.post(
                "/api/tcp-scan/start",
                body,
                {headers: {"Content-Type": "application/json"}}
            );

            if (res.status === 200) {
                openWebSocket();
                toast.info("Scan started");
            } else {
                toast.error("Failed to start scan");
            }
        } catch (err) {
            console.error(err);
            toast.error("Failed to start scan");
        }
    };

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
          
            <CardContent className="space-y-4 max-h-">
                <IpRangeForm onSubmit={startScan} />
                <ScrollArea className="flex-1 rounded-md border p-3 ">
                    <ul className="space-y-2 text-sm">
                        {progress.map((p, i) => (
                            <TcpScanRow
                                key={`${p.Ip}-${p.Port}-${i}`}
                                ip={p.Ip}
                                port={p.Port}
                                status={p.Status}
                                response={p.Response}
                            />
                        ))}
                    </ul>
                </ScrollArea>
            </CardContent>
        </Card>
    );
}

function TcpScanRow({ip, port, status, response}: TcpScanRowProps) {
    const style =
        statusStyles[status] ??
        "bg-muted text-muted-foreground border-border";

    return (
        <li
            className={`flex items-center justify-between gap-4 rounded-lg border px-3 py-2 my-1 ${style}`}
        >
      <span className="font-mono text-sm">
        {ip}:{port}
      </span>

            <div className="flex items-center gap-2">
        <span className="text-xs uppercase tracking-wide">
          {status}
        </span>
                {response && (
                    <span className="text-xs text-muted-foreground truncate max-w-[300px]">
            {response}
          </span>
                )}
            </div>
        </li>
    );
}