import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { Zap, Loader2 } from "lucide-react";

export default function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("admin@youtube-automation.local");
  const [password, setPassword] = useState("admin123");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (api.getToken()) navigate("/");
  }, [navigate]);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      const res = await api.login(email, password);
      api.setToken(res.accessToken);
      navigate("/");
    } catch (err: any) {
      setError(err.message || "Giriş başarısız");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", padding: 20 }}>
      <div className="glass-card animate-fadeIn" style={{ width: "100%", maxWidth: 400, padding: 40 }}>
        <div style={{ textAlign: "center", marginBottom: 32 }}>
          <div className="pulse-glow" style={{
            width: 64, height: 64, borderRadius: 20, background: "var(--accent-gradient)",
            display: "flex", alignItems: "center", justifyContent: "center", margin: "0 auto 20px"
          }}>
            <Zap size={32} color="white" />
          </div>
          <h1 style={{ fontSize: 24, fontWeight: 800, marginBottom: 8 }}>YT Otomasyon</h1>
          <p style={{ color: "var(--text-secondary)", fontSize: 14 }}>Sisteme giriş yapın</p>
        </div>

        {error && (
          <div style={{ padding: 12, background: "rgba(244, 63, 94, 0.1)", border: "1px solid rgba(244, 63, 94, 0.2)", borderRadius: 12, color: "var(--danger)", fontSize: 13, marginBottom: 20, textAlign: "center" }}>
            {error}
          </div>
        )}

        <form onSubmit={handleLogin} style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <div>
            <label style={{ display: "block", fontSize: 12, fontWeight: 600, color: "var(--text-secondary)", marginBottom: 6 }}>Email</label>
            <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div>
            <label style={{ display: "block", fontSize: 12, fontWeight: 600, color: "var(--text-secondary)", marginBottom: 6 }}>Şifre</label>
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </div>
          <button type="submit" className="btn btn-primary" style={{ width: "100%", marginTop: 8 }} disabled={loading}>
            {loading ? <Loader2 size={18} className="animate-spin" /> : "Giriş Yap"}
          </button>
        </form>
      </div>
    </div>
  );
}
