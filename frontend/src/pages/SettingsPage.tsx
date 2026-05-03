import { useEffect, useState } from "react";
import Sidebar from "../components/layout/Sidebar";
import Header from "../components/layout/Header";
import { api } from "../lib/api";
import { Settings as SettingsIcon, Save, Loader2, CheckCircle } from "lucide-react";

export default function SettingsPage() {
  const [settings, setSettings] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [activeTab, setActiveTab] = useState<"api" | "ai" | "system" | "custom">("api");

  useEffect(() => {
    api.getSettings().then(setSettings).finally(() => setLoading(false));
  }, []);

  const handleChange = (key: string, value: string) => {
    setSettings(settings.map(s => s.key === key ? { ...s, value } : s));
  };

  const handleSave = async () => {
    setSaving(true);
    setSaved(false);
    try {
      for (const s of settings) {
        await api.updateSetting(s.key, s.value);
      }
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (e: any) {
      alert("Hata: " + e.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <Sidebar />
      <main className="main-content" style={{ width: "100%" }}>
        <Header title="Sistem Ayarları" />

        <div className="glass-card animate-fadeIn" style={{ padding: 32, maxWidth: 800 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 24, borderBottom: "1px solid var(--border)", paddingBottom: 16 }}>
            <SettingsIcon size={24} color="var(--accent)" />
            <h3 style={{ fontSize: 18, fontWeight: 700 }}>API Anahtarları & Yapılandırma</h3>
          </div>

          <div style={{ borderBottom: "1px solid var(--border)", marginBottom: 20, display: "flex", gap: 4 }}>
            <button className={`tab-btn ${activeTab === "api" ? "active" : ""}`} onClick={() => setActiveTab("api")}>API Anahtarları</button>
            <button className={`tab-btn ${activeTab === "ai" ? "active" : ""}`} onClick={() => setActiveTab("ai")}>AI Modelleri</button>
            <button className={`tab-btn ${activeTab === "system" ? "active" : ""}`} onClick={() => setActiveTab("system")}>Sistem Limitleri</button>
            <button className={`tab-btn ${activeTab === "custom" ? "active" : ""}`} onClick={() => setActiveTab("custom")}>Özel Ayarlar</button>
          </div>

          {loading ? (
            <div style={{ padding: 40, textAlign: "center" }}><Loader2 className="animate-spin" style={{ margin: "0 auto", color: "var(--accent)" }} /></div>
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
              {settings.length === 0 && (
                <div style={{ padding: 20, textAlign: "center", color: "var(--text-secondary)", border: "1px dashed var(--border)", borderRadius: 12 }}>
                  Veritabanında ayar bulunamadı. Lütfen backend'i yeniden başlatarak standart ayarların yüklenmesini sağlayın.
                </div>
              )}
              
              {settings.filter(s => {
                if (activeTab === "api") return s.key.includes("ApiKey") || s.key.includes("Client");
                if (activeTab === "ai") return s.key.includes("Model") || s.key.includes("Provider");
                if (activeTab === "system") return s.key.includes("System") || s.key.includes("Limit");
                return !s.key.includes("ApiKey") && !s.key.includes("Client") && !s.key.includes("Model") && !s.key.includes("System");
              }).map(s => (
                <div key={s.key} style={{ display: "flex", gap: 16, alignItems: "flex-end" }}>
                  <div style={{ flex: 1 }}>
                    <label style={{ display: "block", fontSize: 13, fontWeight: 600, color: "var(--text-secondary)", marginBottom: 8 }}>
                      {s.key}
                    </label>
                    {s.key.toLowerCase().includes('key') || s.key.toLowerCase().includes('secret') || s.key.toLowerCase().includes('password') ? (
                      <input className="input" type="password" value={s.value} onChange={(e) => handleChange(s.key, e.target.value)} />
                    ) : (
                      <input className="input" type="text" value={s.value} onChange={(e) => handleChange(s.key, e.target.value)} />
                    )}
                  </div>
                </div>
              ))}

              <div style={{ marginTop: 24, display: "flex", alignItems: "center", gap: 16, borderBottom: activeTab === "custom" ? "1px solid var(--border)" : "none", paddingBottom: activeTab === "custom" ? 24 : 0 }}>
                <button className="btn btn-primary" onClick={handleSave} disabled={saving} style={{ padding: "12px 24px" }}>
                  {saving ? <Loader2 size={18} className="animate-spin" /> : <Save size={18} />}
                  Ayarları Kaydet
                </button>
                {saved && (
                  <span className="animate-fadeIn" style={{ display: "flex", alignItems: "center", gap: 6, color: "var(--success)", fontSize: 14, fontWeight: 600 }}>
                    <CheckCircle size={16} /> Kaydedildi
                  </span>
                )}
              </div>

              {/* Yeni Ayar Ekleme Formu */}
              {activeTab === "custom" && (
                <div className="animate-fadeIn">
                  <h4 style={{ fontSize: 15, fontWeight: 600, marginBottom: 16 }}>Yeni Özel Ayar Ekle</h4>
                  <div style={{ display: "flex", gap: 12 }}>
                    <input id="newKey" className="input" placeholder="Ayar Adı (Örn: Custom:MySetting)" style={{ flex: 1 }} />
                    <input id="newVal" className="input" placeholder="Ayar Değeri" style={{ flex: 2 }} />
                    <button className="btn btn-secondary" onClick={() => {
                      const k = (document.getElementById("newKey") as HTMLInputElement).value;
                      const v = (document.getElementById("newVal") as HTMLInputElement).value;
                      if(k) {
                        setSettings([...settings, { key: k, value: v, category: k.split(':')[0] || 'general' }]);
                        (document.getElementById("newKey") as HTMLInputElement).value = '';
                        (document.getElementById("newVal") as HTMLInputElement).value = '';
                      }
                    }}>Listeye Ekle</button>
                  </div>
                  <p style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 8 }}>Ekledikten sonra "Ayarları Kaydet" butonuna basmayı unutmayın.</p>
                </div>
              )}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
