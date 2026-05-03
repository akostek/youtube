const API_URL = 'http://localhost:5000/api';

class ApiClient {
  private token: string | null = null;

  setToken(token: string) {
    this.token = token;
    if (typeof window !== 'undefined') localStorage.setItem('yt_token', token);
  }

  getToken(): string | null {
    if (this.token) return this.token;
    if (typeof window !== 'undefined') {
      this.token = localStorage.getItem('yt_token');
    }
    return this.token;
  }

  clearToken() {
    this.token = null;
    if (typeof window !== 'undefined') localStorage.removeItem('yt_token');
  }

  private async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const token = this.getToken();
    const headers: Record<string, string> = { 'Content-Type': 'application/json', ...((options.headers as Record<string, string>) || {}) };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    const res = await fetch(`${API_URL}${path}`, { ...options, headers });
    if (res.status === 401) { this.clearToken(); if (typeof window !== 'undefined') window.location.href = '/login'; throw new Error('Unauthorized'); }
    if (!res.ok) { const err = await res.json().catch(() => ({})); throw new Error(err.message || `HTTP ${res.status}`); }
    return res.json();
  }

  // Auth
  async login(email: string, password: string) { return this.request<{ accessToken: string; user: any }>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }); }

  // Dashboard
  async getDashboardStats() { return this.request<any>('/dashboard/stats'); }
  async getActiveJobs() { return this.request<any[]>('/dashboard/active-jobs'); }
  async getLogs() { return this.request<any[]>('/dashboard/logs'); }

  // Channels
  async getChannels() { return this.request<any[]>('/channels'); }
  async getChannel(id: string) { return this.request<any>(`/channels/${id}`); }
  async updateChannel(id: string, data: any) { return this.request<any>(`/channels/${id}`, { method: 'PUT', body: JSON.stringify(data) }); }
  async deleteChannel(id: string) { return this.request<any>(`/channels/${id}`, { method: 'DELETE' }); }
  getConnectUrl() { return `${API_URL}/channels/oauth/connect`; }

  // Pipelines
  async getPipelines() { return this.request<any[]>('/pipelines'); }
  async createPipeline(data: any) { return this.request<any>('/pipelines', { method: 'POST', body: JSON.stringify(data) }); }
  async updatePipeline(id: string, data: any) { return this.request<any>(`/pipelines/${id}`, { method: 'PUT', body: JSON.stringify(data) }); }
  async startPipeline(id: string) { return this.request<any>(`/pipelines/${id}/start`, { method: 'POST' }); }
  async startAllPipelines() { return this.request<any>('/pipelines/start-all', { method: 'POST' }); }
  async deletePipeline(id: string) { return this.request<any>(`/pipelines/${id}`, { method: 'DELETE' }); }
  async stopPipeline(id: string) { return this.request<any>(`/pipelines/${id}/stop`, { method: 'POST' }); }
  async getPipelineStatus(id: string) { return this.request<any>(`/pipelines/${id}/status`); }

  // Admin
  async getSettings() { return this.request<any[]>('/admin/settings'); }
  async updateSetting(key: string, value: string) { return this.request<any>('/admin/settings', { method: 'PUT', body: JSON.stringify({ key, value }) }); }
  async getHealth() { return this.request<any>('/admin/health'); }

  // Analytics
  async getAnalyticsStats() { return this.request<any>('/analytics/stats'); }
  async getAnalyticsVideos() { return this.request<any[]>('/analytics/videos'); }
}

export const api = new ApiClient();
