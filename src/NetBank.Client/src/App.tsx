import './App.css'
import Dashboard from "@/components/Dashboard.tsx";
import {ThemeProvider} from "@/components/theme/theme-provider"
import {Toaster} from "sonner";


function App() {

    return (
        <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
            <Toaster richColors closeButton/>
            <Dashboard/>
        </ThemeProvider>
    )
}

export default App
