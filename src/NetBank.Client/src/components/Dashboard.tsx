import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import TcpScanLive from "@/components/custom/TcpScanLive.tsx";

export default function Dashboard() {
    return (
        <div className="flex min-h-screen bg-background text-foreground">
            {/* Sidebar */}
            <aside className="w-32 border-r p-4 space-y-6">
                <h2 className="text-xl font-bold">Netbank dashboard</h2>
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
            <main className="col-span-2  flex-1 p-6 space-y-6">
                {/* Top stats */}
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <StatCard title="Total Revenue" value="$1,250.00" badge="+12.5%" />
                </div>
                <TcpScanLive/>
                
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

