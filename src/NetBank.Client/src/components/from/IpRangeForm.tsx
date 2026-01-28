import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "../ui/input";
import {Label} from "@/components/ui/label.tsx";

interface IpRangeFormProps {
    onSubmit?: (body: { IpRangeStart: string; IpRangeEnd: string; Port: number; TimeoutMs: number }) => void;
}

export function IpRangeForm({ onSubmit }: IpRangeFormProps) {
    const [startIp, setStartIp] = useState("");
    const [endIp, setEndIp] = useState("");
    const [port, setPort] = useState(80);
    const [timeout, setTimeout] = useState(1000);
    const [errors, setErrors] = useState<{ [key: string]: string }>({});

    const validateIp = (ip: string) => {
        const regex = /^(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}$/;
        return regex.test(ip);
    };

    const validatePort = (p: number) => p > 0 && p <= 65535;
    const validateTimeout = (t: number) => t > 0;

    const handleSubmit = () => {
        const newErrors: { [key: string]: string } = {};
        if (!validateIp(startIp)) newErrors.startIp = "Invalid start IP";
        if (!validateIp(endIp)) newErrors.endIp = "Invalid end IP";
        if (!validatePort(port)) newErrors.port = "Port must be 1-65535";
        if (!validateTimeout(timeout)) newErrors.timeout = "Timeout must be positive";

        setErrors(newErrors);

        if (Object.keys(newErrors).length === 0) {
            const body = {
                IpRangeStart: startIp,
                IpRangeEnd: endIp,
                Port: port,
                TimeoutMs: timeout,
            };
            console.log("Submitting body:", body);
            if (onSubmit) onSubmit(body);
        }
    };

    return (
        <div className="flex flex-row items-center justify-between ">
            <div>
                <Label>IP Range Start</Label>
                <Input
                    value={startIp}
                    onChange={(e) => setStartIp(e.target.value)}
                    className={errors.startIp ? "border-red-500" : ""}
                    placeholder="192.168.0.1"
                />
                {errors.startIp && <p className="text-red-500 text-sm">{errors.startIp}</p>}
            </div>

            <div>
                <Label>IP Range End</Label>
                <Input
                    value={endIp}
                    onChange={(e) => setEndIp(e.target.value)}
                    className={errors.endIp ? "border-red-500" : ""}
                    placeholder="192.168.0.10"
                />
                {errors.endIp && <p className="text-red-500 text-sm">{errors.endIp}</p>}
            </div>

            <div>
                <Label>Port</Label>
                <Input
                    type="number"
                    value={port}
                    onChange={(e) => setPort(Number(e.target.value))}
                    className={errors.port ? "border-red-500" : ""}
                />
                {errors.port && <p className="text-red-500 text-sm">{errors.port}</p>}
            </div>

            <div>
                <Label>Timeout (ms)</Label>
                <Input
                    type="number"
                    value={timeout}
                    onChange={(e) => setTimeout(Number(e.target.value))}
                    className={errors.timeout ? "border-red-500" : ""}
                />
                {errors.timeout && <p className="text-red-500 text-sm">{errors.timeout}</p>}
            </div>

            <Button onClick={handleSubmit}>Submit</Button>
        </div>
    );
}
