import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { useEffect } from "react";
import DashboardPage from "./pages/DashboardPage";
import ChannelsPage from "./pages/ChannelsPage";
import PipelinesPage from "./pages/PipelinesPage";
import SettingsPage from "./pages/SettingsPage";
import LoginPage from "./pages/LoginPage";
import AnalyticsPage from "./pages/AnalyticsPage";
import { api } from "./lib/api";

function PrivateRoute({ children }: { children: React.ReactNode }) {
  if (!api.getToken()) {
    return <Navigate to="/login" replace />;
  }
  return children;
}

function App() {
  useEffect(() => {
    // Theme init
    const theme = localStorage.getItem('yt_theme') || 'dark';
    document.documentElement.setAttribute('data-theme', theme);
  }, []);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        
        <Route path="/" element={
          <PrivateRoute>
            <DashboardPage />
          </PrivateRoute>
        } />
        
        <Route path="/channels" element={
          <PrivateRoute>
            <ChannelsPage />
          </PrivateRoute>
        } />
        
        <Route path="/pipelines" element={
          <PrivateRoute>
            <PipelinesPage />
          </PrivateRoute>
        } />
        
        <Route path="/analytics" element={
          <PrivateRoute>
            <AnalyticsPage />
          </PrivateRoute>
        } />
        
        <Route path="/settings" element={
          <PrivateRoute>
            <SettingsPage />
          </PrivateRoute>
        } />
        
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
