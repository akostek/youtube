import { Bell, Search, Sun, Moon } from "lucide-react";
import { useEffect, useState } from "react";

export default function Header({ title }: { title: string }) {
  const [theme, setTheme] = useState("dark");

  useEffect(() => {
    const saved = localStorage.getItem("yt_theme") || "dark";
    setTheme(saved);
    document.documentElement.setAttribute("data-theme", saved);
  }, []);

  const toggleTheme = () => {
    const next = theme === "dark" ? "light" : "dark";
    setTheme(next);
    localStorage.setItem("yt_theme", next);
    document.documentElement.setAttribute("data-theme", next);
  };

  return (
    <header className="animate-fadeIn" style={{
      display: "flex", justifyContent: "space-between", alignItems: "center",
      marginBottom: 24, paddingBottom: 20, borderBottom: "1px solid var(--border)",
    }}>
      <div>
        <h2 style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-0.03em" }}>{title}</h2>
        <div style={{ width: 50, height: 3, background: "var(--accent-gradient)", borderRadius: 2, marginTop: 6 }} />
      </div>
      
      <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
        <div style={{ position: "relative" }}>
          <Search size={16} style={{ position: "absolute", left: 12, top: "50%", transform: "translateY(-50%)", color: "var(--text-muted)" }} />
          <input className="input" placeholder="Ara..." style={{ paddingLeft: 38, width: 220, fontSize: 13, padding: "10px 14px 10px 38px" }} />
        </div>
        <button
          onClick={toggleTheme}
          style={{
            width: 40, height: 40, borderRadius: 12, border: "1px solid var(--border)",
            display: "flex", alignItems: "center", justifyContent: "center", cursor: "pointer",
            background: "var(--bg-hover)", transition: "all 0.3s",
          }}
          title={theme === "dark" ? "Açık Tema" : "Koyu Tema"}
        >
          {theme === "dark" ? <Sun size={18} color="var(--warning)" /> : <Moon size={18} color="var(--accent)" />}
        </button>
        <button style={{
          width: 40, height: 40, borderRadius: 12, border: "1px solid var(--border)",
          display: "flex", alignItems: "center", justifyContent: "center", cursor: "pointer",
          background: "var(--bg-hover)", position: "relative",
        }}>
          <Bell size={18} color="var(--text-secondary)" />
        </button>
      </div>
    </header>
  );
}
