import { useEffect, useState, useCallback } from "react";
import Sidebar from "../components/layout/Sidebar";
import Header from "../components/layout/Header";
import { api } from "../lib/api";
import { formatNumber, formatDate } from "../lib/utils";
import {
  Tv2, Video, Upload, Clock, Zap, GitBranch,
  Activity, Loader2, CheckCircle,
  XCircle, Play, FileText, Mic, Film, Send
} from "lucide-react";
import { useNavigate } from "react-router-dom";

const STEP_META: Record<string, { label: string; icon: any; color: string; pct: number }> = {
  TOPIC_GENERATION: { label: "Konu Bulunuyor", icon: FileText, color: "#8b5cf6", pct: 10 },
  SCRIPT_GENERATION: { label: "Senaryo Yazılıyor", icon: FileText, color: "#3b82f6", pct: 30 },
  VOICE_GENERATION: { label: "Seslendiriliyor", icon: Mic, color: "#0ea5e9", pct: 55 },
  VIDEO_RENDERING: { label: "Video Oluşturuluyor", icon: Film, color: "#10b981", pct: 75 },
  YOUTUBE_UPLOAD: { label: "YouTube'a Yükleniyor", icon: Send, color: "#f59e0b", pct: 92 },
};

const STATUS_BADGE: Record<string, { label: string; cls: string; icon: any }> = {
  QUEUED: { label: "Kuyrukta", cls: "badge-warning", icon: Clock },
  PROCESSING: { label: "İşleniyor", cls: "badge-info", icon: Loader2 },
  COMPLETED: { label: "Tamamlandı", cls: "badge-success", icon: CheckCircle },
  FAILED: { label: "Başarısız", cls: "badge-danger", icon: XCircle },
};

export default function DashboardPage() {
  const navigate = useNavigate();
  const [stats, setStats] = useState<any>(null);
  const [activeJobs, setActiveJobs] = useState<any[]>([]);
  const [logs, setLogs] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<"live" | "logs">("live");

  const fetchData = useCallback(async () => {
    try {
      const [s, jobs, l] = await Promise.all([
        api.getDashboardStats(),
        api.getActiveJobs(),
        api.getLogs(),
      ]);
      setStats(s);
      setActiveJobs(jobs);
      setLogs(l);
    } catch {} finally { setLoading(false); }
  }, []);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 5000);
    return () => clearInterval(interval);
  }, [fetchData]);

  const statCards = stats ? [
    { label: "Kanallar", value: stats.channels, icon: Tv2, color: "#8b5cf6" },
    { label: "Aktif Akışlar", value: stats.activePipelines, icon: GitBranch, color: "#3b82f6" },
    { label: "Üretilen Video", value: stats.videosGenerated, icon: Video, color: "#10b981" },
    { label: "Yayınlanan", value: stats.videosPublished, icon: Upload, color: "#f59e0b" },
    { label: "Kuyruk", value: stats.pendingJobs, icon: Clock, color: "#0ea5e9" },
    { label: "Başarısız", value: stats.failedJobs || 0, icon: XCircle, color: "#f43f5e" },
  ] : [];

  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <Sidebar />
      <main className="main-content" style={{ width: "100%" }}>
        <Header title="Kontrol Paneli" />

        <div style={{ display: "grid", gridTemplateColumns: "repeat(6, 1fr)", gap: 16, marginBottom: 24 }}>
          {loading && !stats
            ? Array.from({ length: 6 }).map((_, i) => (
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

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 24 }}>
          <button
            className="glass-card"
            onClick={async () => {
              try { await api.startAllPipelines(); fetchData(); } catch (e: any) { alert(e.message); }
            }}
            style={{ padding: 24, cursor: "pointer", display: "flex", alignItems: "center", gap: 16, textAlign: "left", border: "1px solid rgba(139, 92, 246, 0.2)" }}
          >
            <div className="pulse-glow" style={{ width: 48, height: 48, borderRadius: 14, background: "var(--accent-gradient)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <Zap size={24} color="white" />
            </div>
            <div>
              <h4 style={{ fontSize: 16, fontWeight: 700, marginBottom: 4 }}>Seri Üretimi Başlat</h4>
              <p style={{ fontSize: 13, color: "var(--text-secondary)" }}>Tüm aktif otomasyonları paralel çalıştır</p>
            </div>
          </button>
          <button
            className="glass-card"
            onClick={() => navigate("/pipelines")}
            style={{ padding: 24, cursor: "pointer", display: "flex", alignItems: "center", gap: 16, textAlign: "left", border: "1px solid rgba(16, 185, 129, 0.2)" }}
          >
            <div style={{ width: 48, height: 48, borderRadius: 14, background: "rgba(16, 185, 129, 0.1)", border: "1px solid rgba(16, 185, 129, 0.2)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <GitBranch size={24} color="var(--success)" />
            </div>
            <div>
              <h4 style={{ fontSize: 16, fontWeight: 700, marginBottom: 4 }}>Otomasyon Yönet</h4>
              <p style={{ fontSize: 13, color: "var(--text-secondary)" }}>Akış, zamanlama ve AI ayarlarını düzenle</p>
            </div>
          </button>
        </div>

        <div style={{ borderBottom: "1px solid var(--border)", marginBottom: 20, display: "flex", gap: 4 }}>
          <button className={`tab-btn ${activeTab === "live" ? "active" : ""}`} onClick={() => setActiveTab("live")}>
            <Activity size={14} style={{ marginRight: 6, verticalAlign: "middle" }} /> Canlı Üretim Bandı
          </button>
          <button className={`tab-btn ${activeTab === "logs" ? "active" : ""}`} onClick={() => setActiveTab("logs")}>
            <FileText size={14} style={{ marginRight: 6, verticalAlign: "middle" }} /> İşlem Logları
          </button>
        </div>

        {activeTab === "live" && (
          <div className="glass-card animate-fadeIn" style={{ padding: 24 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
              <h3 style={{ fontSize: 16, fontWeight: 700, display: "flex", alignItems: "center", gap: 8 }}>
                <Activity size={18} color="var(--accent)" /> Canlı Üretim Durumu
              </h3>
              {activeJobs.length > 0 && (
                <span className="badge badge-success" style={{ fontSize: 11 }}>
                  <span style={{ width: 6, height: 6, borderRadius: "50%", background: "var(--success)", display: "inline-block", animation: "pulse 2s infinite" }} />
                  {activeJobs.length} Aktif
                </span>
              )}
            </div>

            {activeJobs.length === 0 ? (
              <div style={{ padding: 40, textAlign: "center", border: "1px dashed var(--border)", borderRadius: 12 }}>
                <Play size={28} color="var(--text-muted)" style={{ margin: "0 auto 12px", display: "block" }} />
                <p style={{ color: "var(--text-secondary)", fontSize: 14 }}>Şu anda çalışan üretim yok. "Seri Üretimi Başlat" ile tetikleyin.</p>
              </div>
            ) : (
              <div style={{ display: "grid", gap: 12 }}>
                {activeJobs.map((job) => {
                  const meta = STEP_META[job.step] || { label: job.step, color: "#666", pct: 50, icon: Activity };
                  const StepIcon = meta.icon;
                  const isProcessing = job.status === 'PROCESSING';
                  
                  let currentPct = meta.pct;
                  let detailText = "";
                  if (job.payload) {
                    try {
                      const payload = JSON.parse(job.payload);
                      if (payload.progress !== undefined) {
                        currentPct = Math.round(payload.progress);
                      }
                      if (payload.detail) {
                        detailText = payload.detail;
                      }
                    } catch {}
                  }

                  return (
                    <div key={job.id} style={{ padding: 16, background: "var(--bg-hover)", borderRadius: 12, border: `1px solid var(--border)`, borderLeft: `3px solid ${meta.color}` }}>
                      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 10 }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                          <StepIcon size={16} color={meta.color} />
                          <span style={{ fontSize: 14, fontWeight: 600 }}>{job.pipeline?.name || "Pipeline"}</span>
                          <span style={{ fontSize: 12, color: "var(--text-muted)" }}>• {job.pipeline?.channel?.title}</span>
                        </div>
                        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                          <span style={{ fontSize: 12, fontWeight: 700, color: meta.color }}>
                            {job.step === 'YOUTUBE_UPLOAD' && isProcessing ? 'YouTube\'a Yükleniyor...' : meta.label}
                          </span>
                          {isProcessing && <Loader2 size={14} className="animate-spin" color={meta.color} />}
                          <span style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)" }}>%{currentPct}</span>
                        </div>
                      </div>
                      <div className="progress-bar" style={{ marginBottom: detailText ? 8 : 0 }}>
                        <div className="progress-bar-fill" style={{ width: `${currentPct}%`, background: `linear-gradient(90deg, ${meta.color}, ${meta.color}88)` }} />
                      </div>
                      {detailText && (
                        <div style={{ fontSize: 11, color: "var(--text-muted)", fontFamily: "monospace", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", background: "rgba(0,0,0,0.2)", padding: "4px 8px", borderRadius: 4 }}>
                          &gt; {detailText}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {activeTab === "logs" && (
          <div className="animate-fadeIn">
            <div className="data-table-container">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Durum</th>
                    <th>Adım</th>
                    <th>Pipeline</th>
                    <th>Kanal</th>
                    <th>Tarih</th>
                    <th>Hata</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.slice(0, 30).map((log) => {
                    const sb = STATUS_BADGE[log.status] || STATUS_BADGE.QUEUED;
                    const SbIcon = sb.icon;
                    const meta = STEP_META[log.step];
                    return (
                      <tr key={log.id}>
                        <td>
                          <span className={`badge ${sb.cls}`} style={{ fontSize: 11, padding: "4px 10px" }}>
                            {log.status === 'PROCESSING' ? <Loader2 size={12} className="animate-spin" /> : <SbIcon size={12} />}
                            {sb.label}
                          </span>
                        </td>
                        <td style={{ fontSize: 13, fontWeight: 600 }}>{meta?.label || log.step}</td>
                        <td style={{ fontSize: 13 }}>{log.pipeline?.name || "-"}</td>
                        <td style={{ fontSize: 13, color: "var(--text-secondary)" }}>{log.pipeline?.channel?.title || "-"}</td>
                        <td style={{ fontSize: 12, color: "var(--text-muted)" }}>{formatDate(log.updatedAt)}</td>
                        <td style={{ fontSize: 12, color: "var(--danger)", maxWidth: 200, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                          {log.errorMessage || "—"}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}
