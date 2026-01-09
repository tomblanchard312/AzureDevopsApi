import { apiClient } from '../../api/client';

export type ChatMode = 'general' | 'code-review' | 'documentation' | 'debugging' | 'planning';

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  mode?: ChatMode;
  confidence?: number;
  sources?: string[];
  proposals?: ChatProposal[];
}

export interface ChatProposal {
  id: string;
  type: string;
  title: string;
  description: string;
  confidence: number;
}

export interface ChatRequest {
  repoKey: string;
  message: string;
  mode: ChatMode;
  context?: {
    currentPage?: string;
    selectedItems?: string[];
  };
}

export interface ChatResponse {
  message: ChatMessage;
  suggestions?: string[];
}

export class RepoChatApi {
  static async sendMessage(request: ChatRequest): Promise<ChatResponse> {
    const response = await apiClient.post<ChatResponse>('/api/chat/repo', request);
    return response.data;
  }

  static async getChatHistory(repoKey: string): Promise<ChatMessage[]> {
    const response = await apiClient.get<ChatMessage[]>(`/api/chat/repo/${repoKey}/history`);
    return response.data;
  }

  static async clearChatHistory(repoKey: string): Promise<void> {
    await apiClient.delete(`/api/chat/repo/${repoKey}/history`);
  }
}