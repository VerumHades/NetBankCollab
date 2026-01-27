import './App.css'
import Dashboard from "@/components/Dashboard.tsx";
import { ThemeProvider } from "@/components/theme/theme-provider"

function App() {

  return (
      <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
        <Dashboard/>
      </ThemeProvider>
  )
}

export default App
