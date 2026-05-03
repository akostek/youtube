import { Link, useLocation } from "react-router-dom";
import { LayoutDashboard, Tv2, GitBranch, Settings, Zap, LogOut, BarChart3 } from "lucide-react";
import { api } from "../../lib/api";

const navItems = [
  { href: "/", label: "Kontrol Paneli", icon: LayoutDashboard },
  { href: "/analytics", label: "Analizler", icon: BarChart3 },
  { href: "/channels", label: "Kanallar", icon: Tv2 },
  { href: "/pipelines", label: "Otomasyon", icon: GitBranch },
  { href: "/settings", label: "Ayarlar", icon: Settings },
];

export default function Sidebar() {
  const location = useLocation();
  const pathname = location.pathname;

  return (
    <aside className="sidebar">
      {/* Logo */}
      <div style={{ padding: "20px 16px", borderBottom: "1px solid var(--border)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          <div style={{
            width: 38, height: 38, borderRadius: 12,
            background: "var(--accent-gradient)",
            boxShadow: "0 4px 15px var(--accent-glow)",
            display: "flex", alignItems: "center", justifyContent: "center",
          }}>
            <Zap size={20} color="white" />
          </div>
          <div>
            <h1 style={{ fontSize: 16, fontWeight: 800, letterSpacing: "-0.02em" }} className="gradient-text">
              YT Otomasyon
            </h1>
            <p style={{ fontSize: 11, color: "var(--text-muted)", fontWeight: 500 }}>AI İçerik Fabrikası</p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav style={{ flex: 1, padding: "12px 0" }}>
        {navItems.map((item) => {
          const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
          const Icon = item.icon;
          return (
            <Link key={item.href} to={item.href} className={`sidebar-link ${isActive ? "active" : ""}`}>
              <Icon size={18} />
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>

      {/* Footer */}
      <div style={{ padding: "16px", borderTop: "1px solid var(--border)" }}>
        <button
          onClick={() => { api.clearToken(); window.location.href = "/login"; }}
          className="sidebar-link" style={{ width: "100%", margin: 0, cursor: "pointer", background: "none", border: "none", textAlign: "left", color: "var(--danger)" }}
        >
          <LogOut size={18} />
          <span style={{ fontWeight: 600 }}>Çıkış Yap</span>
        </button>
      </div>
    </aside>
  );
}
