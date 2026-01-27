import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import { Progress } from "@/components/ui/progress";
import { MoreHorizontal } from "lucide-react";
import TcpScanLive from "@/components/custom/TcpScanLive.tsx";

export default function Dashboard() {
    return (
        <div className="flex min-h-screen bg-background text-foreground">
            {/* Sidebar */}
            <aside className="w-64 border-r p-4 space-y-6">
                <h2 className="text-xl font-bold">Acme Inc.</h2>
                <nav className="space-y-2 text-sm">
                    {[
                        "Dashboard",
                        "Analytics",
                        "Team",
                    ].map((item) => (
                        <div
                            key={item}
                            className="px-3 py-2 rounded-lg hover:bg-muted cursor-pointer"
                        >
                            {item}
                        </div>
                    ))}
                </nav>
            </aside>

            {/* Main */}
            <main className="flex-1 p-6 space-y-6">
                {/* Top stats */}
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <StatCard title="Total Revenue" value="$1,250.00" badge="+12.5%" />
                </div>
                <TcpScanLive/>

                {/* Table */}
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between">
                        <CardTitle>Documents</CardTitle>
                        <Button size="sm">Add Section</Button>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-2">
                            <Row title="Cover page" status="In Process" target={18} limit={5} />
                            <Row title="Table of contents" status="Done" target={29} limit={24} />
                            <Row title="Executive summary" status="Done" target={10} limit={13} />
                            <Row title="Technical approach" status="Done" target={27} limit={23} />
                        </div>
                    </CardContent>
                </Card>
            </main>
        </div>
    );
}

// @ts-ignore
function StatCard({ title, value, badge }) {
    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="text-sm font-medium">{title}</CardTitle>
                <Badge variant="secondary">{badge}</Badge>
            </CardHeader>
            <CardContent>
                <div className="text-2xl font-bold">{value}</div>
            </CardContent>
        </Card>
    );
}

// @ts-ignore
function Row({ title, status, target, limit }) {
    const done = status === "Done";
    return (
        <div className="flex items-center gap-4 p-2 rounded-lg hover:bg-muted">
            <Checkbox />
            <div className="flex-1">
                <div className="font-medium">{title}</div>
                <Progress value={(limit / target) * 100} />
            </div>
            <Badge variant={done ? "default" : "outline"}>{status}</Badge>
            <div className="text-sm w-20 text-right">
                {limit}/{target}
            </div>
            <Button size="icon" variant="ghost">
                <MoreHorizontal className="w-4 h-4" />
            </Button>
        </div>
    );
}
