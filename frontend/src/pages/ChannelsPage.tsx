import { useEffect, useState } from "react";
import Sidebar from "../components/layout/Sidebar";
import Header from "../components/layout/Header";
import { api } from "../lib/api";
import { Tv2, Plus, Loader2, Link2, Trash2 } from "lucide-react";
import { formatDate } from "../lib/utils";

export default function ChannelsPage() {
  const [channels, setChannels] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getChannels().then(setChannels).finally(() => setLoading(false));
  }, []);

  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <Sidebar />
      <main className="main-content" style={{ width: "100%" }}>
        <Header title="Kanal Yönetimi" />

        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
          <p style={{ color: "var(--text-secondary)", fontSize: 14 }}>
            <strong style={{ color: "var(--text-primary)" }}>{channels.length}</strong> bağlı kanal
          </p>
          <a href={api.getConnectUrl()} className="btn btn-primary" style={{ textDecoration: "none", fontSize: 13, padding: "10px 20px" }}>
            <Plus size={16} /> Yeni Kanal Bağla
          </a>
        </div>

        {loading ? (
          <div className="glass-card" style={{ padding: 60, textAlign: "center" }}>
            <Loader2 size={32} className="animate-spin" style={{ margin: "0 auto", color: "var(--accent)" }} />
          </div>
        ) : channels.length === 0 ? (
          <div className="glass-card animate-fadeIn" style={{ padding: 48, textAlign: "center", borderStyle: "dashed" }}>
            <Tv2 size={28} color="var(--accent)" style={{ margin: "0 auto 12px" }} />
            <h3 style={{ fontSize: 18, fontWeight: 700, marginBottom: 6 }}>Henüz kanal bağlı değil</h3>
            <p style={{ color: "var(--text-secondary)", fontSize: 13, marginBottom: 20 }}>YouTube hesabınızı bağlayarak otomasyona başlayın.</p>
            <a href={api.getConnectUrl()} className="btn btn-primary" style={{ textDecoration: "none", fontSize: 13 }}>
              <Link2 size={16} /> YouTube ile Bağlan
            </a>
          </div>
        ) : (
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(300px, 1fr))", gap: 16 }}>
            {channels.map((c, i) => (
              <div key={c.id} className="glass-card animate-fadeIn" style={{ padding: 20, animationDelay: `${i * 0.05}s` }}>
                <div style={{ display: "flex", alignItems: "center", gap: 16, marginBottom: 16 }}>
                  {c.thumbnailUrl ? (
                    <img src={c.thumbnailUrl} alt={c.title} style={{ width: 48, height: 48, borderRadius: "50%", border: "2px solid var(--border)" }} />
                  ) : (
                    <div style={{ width: 48, height: 48, borderRadius: "50%", background: "var(--accent-gradient)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                      <Tv2 size={24} color="white" />
                    </div>
                  )}
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <h4 style={{ fontSize: 16, fontWeight: 700, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{c.title}</h4>
                    <p style={{ fontSize: 12, color: "var(--text-muted)" }}>Bağlanma: {formatDate(c.createdAt)}</p>
                  </div>
                </div>
                
                <div style={{ display: "flex", justifyContent: "space-between", padding: "12px 0", borderTop: "1px solid var(--border)", borderBottom: "1px solid var(--border)", marginBottom: 16 }}>
                  <div style={{ textAlign: "center", flex: 1 }}>
                    <div style={{ fontSize: 18, fontWeight: 800 }}>{c.videoCount}</div>
                    <div style={{ fontSize: 11, color: "var(--text-muted)", textTransform: "uppercase" }}>Video</div>
                  </div>
                  <div style={{ width: 1, background: "var(--border)" }} />
                  <div style={{ textAlign: "center", flex: 1 }}>
                    <div style={{ fontSize: 18, fontWeight: 800 }}>{c.uploadCount}</div>
                    <div style={{ fontSize: 11, color: "var(--text-muted)", textTransform: "uppercase" }}>Yükleme</div>
                  </div>
                </div>

                <div style={{ display: "flex", gap: 8 }}>
                  <span className={`badge ${c.isActive ? 'badge-success' : 'badge-danger'}`} style={{ flex: 1, justifyContent: "center" }}>
                    {c.isActive ? 'Aktif' : 'Token Süresi Doldu'}
                  </span>
                  <button className="btn btn-danger" style={{ padding: "8px 12px" }} onClick={async () => {
                    if (confirm("Kanal ve tüm içerikleri silinecek. Emin misiniz?")) {
                      await api.deleteChannel(c.id);
                      api.getChannels().then(setChannels);
                    }
                  }}>
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
