import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

const API_BASE = '/api';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${url}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });
  if (!res.ok) throw new Error(`${res.status}: ${res.statusText}`);
  if (res.status === 204) return undefined as T;
  return res.json();
}

// Entity types
export interface WorldEntity {
  id: string;
  name: string;
  entityType: string;
  description?: string;
  content?: string;
  tags: string[];
  metadata: Record<string, string>;
  createdAt: string;
  updatedAt: string;
}

export interface EntityListItem {
  id: string;
  name: string;
  entityType: string;
  tags: string[];
  description?: string;
  updatedAt: string;
}

export interface SourceReference {
  entityId: string;
  entityName: string;
  entityType: string;
  sectionHeading?: string;
  score: number;
}

// Entity API
export const entities = {
  list: (type?: string, search?: string) => {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    if (search) params.set('search', search);
    const qs = params.toString();
    return fetchJson<EntityListItem[]>(`/entities${qs ? `?${qs}` : ''}`);
  },
  get: (id: string) => fetchJson<WorldEntity>(`/entities/${id}`),
  create: (data: { name: string; entityType: string; description?: string; content?: string; tags?: string[] }) =>
    fetchJson<WorldEntity>('/entities', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: string, data: Partial<{ name: string; entityType: string; description: string; content: string; tags: string[] }>) =>
    fetchJson<WorldEntity>(`/entities/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: string) => fetchJson<void>(`/entities/${id}`, { method: 'DELETE' }),
  types: () => fetchJson<string[]>('/entities/types'),
};

// Import API
export const imports = {
  markdown: (data: { content: string; name?: string; entityType?: string; description?: string; tags?: string[] }) =>
    fetchJson<WorldEntity>('/import/markdown', { method: 'POST', body: JSON.stringify(data) }),
};

// Conversation API
export interface ConversationListItem {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export const conversations = {
  list: () => fetchJson<ConversationListItem[]>('/conversations'),
  get: (id: string) => fetchJson<any>(`/conversations/${id}`),
  create: (title?: string) =>
    fetchJson<any>('/conversations', { method: 'POST', body: JSON.stringify({ title }) }),
  delete: (id: string) => fetchJson<void>(`/conversations/${id}`, { method: 'DELETE' }),
};

// SignalR Chat Connection
export function createChatConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl('/hubs/chat')
    .withAutomaticReconnect()
    .build();
}
