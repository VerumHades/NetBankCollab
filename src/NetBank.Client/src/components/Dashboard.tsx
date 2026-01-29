import {Card, CardContent, CardHeader, CardTitle} from "@/components/ui/card";
import {Badge} from "@/components/ui/badge";
import TcpScanLive from "@/components/custom/TcpScanLive.tsx";
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs.tsx";
import {AccountPanel} from "@/components/pages/Accounts.tsx";

export default function Dashboard() {
    return (
        <Tabs defaultValue="account" orientation="vertical">
            <div className="flex grid-cols-6 min-h-screen bg-background text-foreground">
                {/* Sidebar */}
                <aside className="w-1/5 border-r p-4 space-y-6"> 
                    <h2 className="text-xl font-bold">Netbank dashboard</h2>
                    <TabsList>
                        <TabsTrigger value="tcp">TcpScan</TabsTrigger>
                        <TabsTrigger value="accounts">Accounts</TabsTrigger>
                        <TabsTrigger value="notifications">Notifications</TabsTrigger>
                    </TabsList>
                </aside>
                {/* Main */}
                <main className="col-span-5 p-6 w-full">
                    <TabsContent value="tcp" className={"w-full"} forceMount>
                        <TcpScanLive/>
                        <AccountPanel/>
                    </TabsContent>
                    <TabsContent value="account" className={"w-full"} forceMount>
                  
                    </TabsContent>
                </main>
            </div>
                
        </Tabs>
    );
}

// @ts-ignore
function StatCard({title, value, badge}) {
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

