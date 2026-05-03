import { useEffect, useState } from "react";
import Sidebar from "../components/layout/Sidebar";
import Header from "../components/layout/Header";
import { api } from "../lib/api";
import { formatNumber, formatDate } from "../lib/utils";
import { BarChart3, Eye, ThumbsUp, MessageSquare, Loader2, Play } from "lucide-react";

export default function AnalyticsPage() {
  const [stats, setStats] = useState<any>(null);
  const [videos, setVideos] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      api.getAnalyticsStats(),
      api.getAnalyticsVideos()
    ]).then(([s, v]) => {
      setStats(s);
      setVideos(v);
    }).finally(() => setLoading(false));
  }, []);

  const statCards = stats ? [
    { label: "Toplam İzlenme", value: stats.totalViews, icon: Eye, color: "#3b82f6" },
    { label: "Toplam Beğeni", value: stats.totalLikes, icon: ThumbsUp, color: "#10b981" },
    { label: "Toplam Yorum", value: stats.totalComments, icon: MessageSquare, color: "#8b5cf6" },
    { label: "Yayınlanan Video", value: stats.publishedCount, icon: Play, color: "#f59e0b" },
  ] : [];

  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <Sidebar />
      <main className="main-content" style={{ width: "100%" }}>
        <Header title="Analiz ve İstatistikler" />

        <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 16, marginBottom: 24 }}>
          {loading && !stats
            ? Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="glass-card" style={{ padding: 20, minHeight: 100 }}>
                  <div style={{ width: "50%", height: 12, background: "var(--bg-hover)", borderRadius: 6, marginBottom: 12 }} />
                  <div style={{ width: "40%", height: 28, background: "var(--bg-hover)", borderRadius: 6 }} />
                </div>
              ))
            : statCards.map((card, i) => {
                const Icon = card.icon;
                return (
                  <div key={i} className="glass-card" style={{ padding: 20 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
                      <span style={{ fontSize: 11, color: "var(--text-muted)", fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.06em" }}>{card.label}</span>
                      <Icon size={16} color={card.color} />
                    </div>
                    <span style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-0.02em" }}>{formatNumber(card.value)}</span>
                  </div>
                );
              })}
        </div>

        <div className="glass-card animate-fadeIn" style={{ padding: 24 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
            <h3 style={{ fontSize: 16, fontWeight: 700, display: "flex", alignItems: "center", gap: 8 }}>
              <BarChart3 size={18} color="var(--accent)" /> Video Performansları
            </h3>
          </div>

          {loading ? (
            <div style={{ padding: 40, textAlign: "center" }}><Loader2 className="animate-spin" style={{ margin: "0 auto", color: "var(--accent)" }} /></div>
          ) : videos.length === 0 ? (
            <div style={{ padding: 40, textAlign: "center", border: "1px dashed var(--border)", borderRadius: 12 }}>
              <p style={{ color: "var(--text-secondary)", fontSize: 14 }}>Henüz yayınlanan video istatistiği bulunmuyor.</p>
            </div>
          ) : (
            <div className="data-table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Video Başlığı</th>
                    <th>Kanal</th>
                    <th>Tarih</th>
                    <th style={{ textAlign: "right" }}>İzlenme</th>
                    <th style={{ textAlign: "right" }}>Beğeni</th>
                    <th style={{ textAlign: "right" }}>Yorum</th>
                  </tr>
                </thead>
                <tbody>
                  {videos.map((v) => (
                    <tr key={v.id}>
                      <td style={{ fontSize: 13, fontWeight: 600 }}>
                        <a href={`https://youtube.com/watch?v=${v.youtubeVideoId}`} target="_blank" rel="noreferrer" style={{ color: "var(--text-primary)", textDecoration: "none" }}>
                          {v.title}
                        </a>
                      </td>
                      <td style={{ fontSize: 13, color: "var(--text-secondary)" }}>{v.channelTitle}</td>
                      <td style={{ fontSize: 12, color: "var(--text-muted)" }}>{formatDate(v.createdAt)}</td>
                      <td style={{ fontSize: 13, fontWeight: 700, textAlign: "right", color: "var(--info)" }}>{formatNumber(v.views)}</td>
                      <td style={{ fontSize: 13, fontWeight: 700, textAlign: "right", color: "var(--success)" }}>{formatNumber(v.likes)}</td>
                      <td style={{ fontSize: 13, fontWeight: 700, textAlign: "right", color: "var(--accent)" }}>{formatNumber(v.comments)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
