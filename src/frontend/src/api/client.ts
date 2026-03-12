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
  relationshipsFrom?: Relationship[];
  relationshipsTo?: Relationship[];
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

export interface Relationship {
  id: string;
  fromEntityId: string;
  fromEntityName: string;
  toEntityId: string;
  toEntityName: string;
  relationshipType: string;
  description?: string;
}

export interface NoteProposal {
  id: string;
  proposalType: 'update' | 'create';
  targetEntityName?: string;
  newEntityName?: string;
  newEntityType?: string;
  originalContent?: string;
  proposedContent: string;
  description?: string;
  status: 'pending' | 'approved' | 'rejected';
  createdAt?: string;
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

// Relationship API
export const relationships = {
  forEntity: (entityId: string) => fetchJson<Relationship[]>(`/relationships/entity/${entityId}`),
  create: (data: { fromEntityId: string; toEntityId: string; relationshipType: string; description?: string }) =>
    fetchJson<Relationship>('/relationships', { method: 'POST', body: JSON.stringify(data) }),
  delete: (id: string) => fetchJson<void>(`/relationships/${id}`, { method: 'DELETE' }),
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

export interface ConversationDetail {
  id: string;
  title: string;
  messages: { id: string; role: string; content: string; referencedChunkIds?: string[]; createdAt: string }[];
}

export const conversations = {
  list: () => fetchJson<ConversationListItem[]>('/conversations'),
  get: (id: string) => fetchJson<ConversationDetail>(`/conversations/${id}`),
  create: (title?: string) =>
    fetchJson<ConversationDetail>('/conversations', { method: 'POST', body: JSON.stringify({ title }) }),
  update: (id: string, data: { title?: string }) =>
    fetchJson<ConversationDetail>(`/conversations/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id: string) => fetchJson<void>(`/conversations/${id}`, { method: 'DELETE' }),
};

// Proposal API
export const proposals = {
  list: (status?: string) => {
    const qs = status ? `?status=${status}` : '';
    return fetchJson<NoteProposal[]>(`/proposals${qs}`);
  },
  approve: (id: string, editedContent?: string) =>
    fetchJson<NoteProposal>(`/proposals/${id}/approve`, {
      method: 'POST',
      body: JSON.stringify({ editedContent }),
    }),
  reject: (id: string) =>
    fetchJson<NoteProposal>(`/proposals/${id}/reject`, { method: 'POST' }),
  bulkApprove: (ids: string[]) =>
    fetchJson<{ approved: number }>('/proposals/bulk-approve', {
      method: 'POST',
      body: JSON.stringify({ ids }),
    }),
  bulkReject: (ids: string[]) =>
    fetchJson<{ rejected: number }>('/proposals/bulk-reject', {
      method: 'POST',
      body: JSON.stringify({ ids }),
    }),
};

// SignalR Chat Connection
export function createChatConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl('/hubs/chat')
    .withAutomaticReconnect()
    .build();
}
