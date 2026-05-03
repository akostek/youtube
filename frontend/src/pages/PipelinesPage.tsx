import { useEffect, useState } from "react";
import Sidebar from "../components/layout/Sidebar";
import Header from "../components/layout/Header";
import { api } from "../lib/api";
import { Play, Pause, Plus, GitBranch, Loader2, Sparkles, Trash2, Clock } from "lucide-react";

export default function PipelinesPage() {
  const [pipelines, setPipelines] = useState<any[]>([]);
  const [channels, setChannels] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState({
    name: "", channelId: "", scriptStyle: "CURIOSITY_HOOK", language: "tr",
    frequencyInMinutes: 60, voiceId: "tr-TR-AhmetNeural",
    topicPrompt: "", targetAudience: "", aiProvider: "openai", videoType: "stock",
    scheduleStartHour: 9, scheduleEndHour: 21,
  });

  useEffect(() => {
    Promise.all([api.getPipelines(), api.getChannels()]).then(([p, c]) => { setPipelines(p); setChannels(c); }).finally(() => setLoading(false));
  }, []);

  const openCreateForm = () => {
    setEditingId(null);
    setForm({
      name: "", channelId: channels[0]?.id || "", scriptStyle: "CURIOSITY_HOOK", language: "tr",
      frequencyInMinutes: 60, voiceId: "tr-TR-AhmetNeural",
      topicPrompt: "", targetAudience: "", aiProvider: "openai", videoType: "stock",
      scheduleStartHour: 9, scheduleEndHour: 21,
    });
    setShowForm(true);
  };

  const openEditForm = (p: any) => {
    setEditingId(p.id);
    setForm({
      name: p.name, channelId: p.channel?.id || "", scriptStyle: p.scriptStyle, language: p.language,
      frequencyInMinutes: p.frequencyInMinutes || 60, voiceId: p.voiceId,
      topicPrompt: p.topicPrompt || "", targetAudience: p.targetAudience || "", aiProvider: p.aiProvider, videoType: p.videoType,
      scheduleStartHour: p.scheduleStartHour, scheduleEndHour: p.scheduleEndHour,
    });
    setShowForm(true);
  };

  const submitForm = async () => {
    if (!form.name || !form.channelId) return;
    if (editingId) {
      await api.updatePipeline(editingId, form);
    } else {
      await api.createPipeline(form);
    }
    setPipelines(await api.getPipelines());
    setShowForm(false);
  };

  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <Sidebar />
      <main className="main-content" style={{ width: "100%" }}>
        <Header title="Otomasyon Yönetimi" />

        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
          <p style={{ color: "var(--text-secondary)", fontSize: 14 }}>
            <strong style={{ color: "var(--text-primary)" }}>{pipelines.length}</strong> otomasyon akışı
          </p>
          <div style={{ display: "flex", gap: 10 }}>
            <button className="btn btn-primary" onClick={async () => { await api.startAllPipelines(); alert("Tüm akışlar başlatıldı!"); }} style={{ fontSize: 13, padding: "10px 20px" }}>
              <Play size={16} /> Tümünü Başlat
            </button>
            <button className="btn btn-secondary" onClick={openCreateForm} style={{ fontSize: 13, padding: "10px 20px" }}>
              <Plus size={16} /> Yeni Akış
            </button>
          </div>
        </div>

        {showForm && (
          <div className="glass-card animate-fadeIn" style={{ padding: 28, marginBottom: 32, border: "1px solid var(--accent-glow)" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 24 }}>
              <Sparkles size={18} color="var(--accent)" />
              <h3 style={{ fontSize: 18, fontWeight: 700 }}>{editingId ? "Otomasyon Akışını Düzenle" : "Yeni Otomasyon Akışı"}</h3>
            </div>
            
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 16, marginBottom: 16 }}>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Akış Adı</label>
                <input className="input" placeholder="Örn: Tarih Shorts" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} style={{ fontSize: 13 }} />
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Kanal</label>
                <select className="input" value={form.channelId} onChange={(e) => setForm({ ...form, channelId: e.target.value })} style={{ fontSize: 13, appearance: "none" }} disabled={!!editingId}>
                  <option value="">Kanal Seçin</option>
                  {channels.map((c) => <option key={c.id} value={c.id}>{c.title}</option>)}
                </select>
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>AI Sağlayıcı</label>
                <select className="input" value={form.aiProvider} onChange={(e) => setForm({ ...form, aiProvider: e.target.value })} style={{ fontSize: 13, appearance: "none" }}>
                  <option value="openai">OpenAI (GPT-4o)</option>
                  <option value="gemini">Google Gemini (Ücretsiz)</option>
                </select>
              </div>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 16, marginBottom: 16 }}>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Senaryo Stili</label>
                <select className="input" value={form.scriptStyle} onChange={(e) => setForm({ ...form, scriptStyle: e.target.value })} style={{ fontSize: 13, appearance: "none" }}>
                  <option value="CURIOSITY_HOOK">Merak Uyandırıcı</option>
                  <option value="STORYTELLING">Hikaye Anlatımı</option>
                  <option value="MOTIVATIONAL">Motivasyonel</option>
                  <option value="EDUCATIONAL">Eğitici</option>
                </select>
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Video Tipi</label>
                <select className="input" value={form.videoType} onChange={(e) => setForm({ ...form, videoType: e.target.value })} style={{ fontSize: 13, appearance: "none" }}>
                  <option value="stock">Stock Video Arka Plan</option>
                  <option value="text_overlay">Yazılı / Text Overlay</option>
                </select>
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>İçerik Dili</label>
                <select className="input" value={form.language} onChange={(e) => {
                  const lang = e.target.value;
                  setForm({ ...form, language: lang, voiceId: lang === 'tr' ? 'tr-TR-AhmetNeural' : 'en-US-ChristopherNeural' });
                }} style={{ fontSize: 13, appearance: "none" }}>
                  <option value="tr">Türkçe</option>
                  <option value="en">İngilizce</option>
                </select>
              </div>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr", gap: 16, marginBottom: 16 }}>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Çalışma Sıklığı</label>
                <select className="input" value={form.frequencyInMinutes} onChange={(e) => setForm({ ...form, frequencyInMinutes: parseInt(e.target.value) || 60 })} style={{ fontSize: 13, appearance: "none" }}>
                  <option value={5}>Her 5 Dakikada Bir</option>
                  <option value={15}>Her 15 Dakikada Bir</option>
                  <option value={30}>Her 30 Dakikada Bir</option>
                  <option value={60}>Her Saat Başı</option>
                  <option value={480}>Günde 3 Kez (8 Saatte Bir)</option>
                  <option value={1440}>Günde 1 Kez</option>
                </select>
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>
                  <Clock size={12} style={{ marginRight: 4, verticalAlign: "middle" }} />Başlama Saati
                </label>
                <input className="input" type="number" min="0" max="23" value={form.scheduleStartHour} onChange={(e) => setForm({ ...form, scheduleStartHour: parseInt(e.target.value) || 0 })} style={{ fontSize: 13 }} />
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>
                  <Clock size={12} style={{ marginRight: 4, verticalAlign: "middle" }} />Bitiş Saati
                </label>
                <input className="input" type="number" min="0" max="23" value={form.scheduleEndHour} onChange={(e) => setForm({ ...form, scheduleEndHour: parseInt(e.target.value) || 23 })} style={{ fontSize: 13 }} />
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Ses Seçimi</label>
                <select className="input" value={form.voiceId} onChange={(e) => setForm({ ...form, voiceId: e.target.value })} style={{ fontSize: 13, appearance: "none" }}>
                  <optgroup label="Türkçe">
                    <option value="tr-TR-AhmetNeural">Ahmet (Erkek)</option>
                    <option value="tr-TR-EmelNeural">Emel (Kadın)</option>
                  </optgroup>
                  <optgroup label="English">
                    <option value="en-US-ChristopherNeural">Christopher</option>
                    <option value="en-US-JennyNeural">Jenny</option>
                  </optgroup>
                </select>
              </div>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 16 }}>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Konu Yönlendirmesi</label>
                <input className="input" placeholder="Örn: Osmanlı tarihi, bilim, uzay" value={form.topicPrompt} onChange={(e) => setForm({ ...form, topicPrompt: e.target.value })} style={{ fontSize: 13 }} />
              </div>
              <div>
                <label style={{ fontSize: 12, color: "var(--text-muted)", fontWeight: 600, display: "block", marginBottom: 6 }}>Hedef Kitle</label>
                <input className="input" placeholder="Örn: 18-35 yaş, tarih meraklıları" value={form.targetAudience} onChange={(e) => setForm({ ...form, targetAudience: e.target.value })} style={{ fontSize: 13 }} />
              </div>
            </div>

            <div style={{ display: "flex", gap: 10, marginTop: 20, paddingTop: 16, borderTop: "1px solid var(--border)" }}>
              <button className="btn btn-primary" onClick={submitForm} style={{ fontSize: 13, padding: "10px 24px" }}>{editingId ? "Güncelle" : "Oluştur"}</button>
              <button className="btn btn-secondary" onClick={() => setShowForm(false)} style={{ fontSize: 13, padding: "10px 24px" }}>İptal</button>
            </div>
          </div>
        )}

        {loading ? (
          <div className="glass-card" style={{ padding: 60, textAlign: "center" }}>
            <Loader2 size={32} className="animate-spin" style={{ margin: "0 auto", color: "var(--accent)" }} />
          </div>
        ) : pipelines.length === 0 && !showForm ? (
          <div className="glass-card animate-fadeIn" style={{ padding: 48, textAlign: "center", borderStyle: "dashed" }}>
            <GitBranch size={28} color="var(--accent)" style={{ margin: "0 auto 12px" }} />
            <h3 style={{ fontSize: 18, fontWeight: 700, marginBottom: 6 }}>Henüz akış yok</h3>
            <p style={{ color: "var(--text-secondary)", fontSize: 13, marginBottom: 20 }}>Otomatik üretim için yeni bir akış oluşturun.</p>
            <button className="btn btn-primary" onClick={openCreateForm} style={{ fontSize: 13 }}>
              <Plus size={16} /> Yeni Akış Oluştur
            </button>
          </div>
        ) : (
          <div style={{ display: "grid", gap: 12 }}>
            {pipelines.map((p, i) => (
              <div key={p.id} className="glass-card animate-fadeIn" style={{ padding: 20, display: "flex", alignItems: "center", gap: 16, animationDelay: `${i * 0.05}s` }}>
                <div style={{ width: 44, height: 44, borderRadius: 14, background: p.isActive ? "rgba(16,185,129,0.1)" : "rgba(245,158,11,0.1)", border: `1px solid ${p.isActive ? "rgba(16,185,129,0.2)" : "rgba(245,158,11,0.2)"}`, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                  <GitBranch size={20} color={p.isActive ? "var(--success)" : "var(--warning)"} />
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <h4 style={{ fontSize: 15, fontWeight: 700, marginBottom: 4 }}>{p.name}</h4>
                  <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 12, color: "var(--text-muted)", flexWrap: "wrap" }}>
                    <span style={{ color: "var(--text-secondary)" }}>{p.channel?.title || "—"}</span>
                    <span>•</span>
                    <span>{p.scriptStyle?.replace("_", " ")}</span>
                    <span>•</span>
                    <span>{p.language?.toUpperCase()}</span>
                    <span>•</span>
                    <span>{p.frequencyInMinutes} dakikada bir</span>
                  </div>
                </div>
                <span className={`badge ${p.isActive ? "badge-success" : "badge-warning"}`} style={{ fontSize: 11, padding: "5px 12px" }}>
                  {p.isActive ? "Aktif" : "Duraklı"}
                </span>
                <div style={{ display: "flex", gap: 8 }}>
                  <button className="btn btn-secondary" style={{ padding: "8px 14px", fontSize: 12 }} onClick={() => openEditForm(p)}>
                    Düzenle
                  </button>
                  {p.isActive ? (
                    <>
                      <button className="btn btn-success" style={{ padding: "8px 14px", fontSize: 12 }} onClick={async () => { await api.startPipeline(p.id); alert("Üretim başlatıldı!"); }}>
                        <Play size={14} /> Üret
                      </button>
                      <button className="btn btn-secondary" style={{ padding: "8px 14px", fontSize: 12 }} onClick={async () => { await api.stopPipeline(p.id); setPipelines(await api.getPipelines()); }}>
                        <Pause size={14} />
                      </button>
                    </>
                  ) : (
                    <button className="btn btn-success" style={{ padding: "8px 14px", fontSize: 12 }} onClick={async () => { await api.startPipeline(p.id); setPipelines(await api.getPipelines()); }}>
                      <Play size={14} /> Başlat
                    </button>
                  )}
                  <button className="btn btn-danger" style={{ padding: "8px 14px", fontSize: 12 }} onClick={async () => {
                    if (confirm("Akış ve tüm içerikler silinecek. Emin misiniz?")) {
                      await api.deletePipeline(p.id);
                      setPipelines(await api.getPipelines());
                    }
                  }}>
                    <Trash2 size={14} />
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
