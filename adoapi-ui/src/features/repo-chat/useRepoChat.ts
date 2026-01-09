import { useState, useCallback } from 'react';
import type { ChatMessage, ChatRequest, ChatMode } from './RepoChatApi';
import { RepoChatApi } from './RepoChatApi';

export interface UseRepoChatOptions {
  repoKey: string;
  onMessageSent?: (message: ChatMessage) => void;
  onError?: (error: Error) => void;
}

export interface UseRepoChatReturn {
  messages: ChatMessage[];
  isLoading: boolean;
  error: string | null;
  sendMessage: (message: string, mode: ChatMode) => Promise<void>;
  clearMessages: () => void;
  loadHistory: () => Promise<void>;
}

// Overload for simple usage with just repoKey string
export function useRepoChat(repoKey: string): UseRepoChatReturn;
export function useRepoChat(options: UseRepoChatOptions): UseRepoChatReturn;
export function useRepoChat(optionsOrRepoKey: UseRepoChatOptions | string): UseRepoChatReturn {
  const options: UseRepoChatOptions = typeof optionsOrRepoKey === 'string'
    ? { repoKey: optionsOrRepoKey }
    : optionsOrRepoKey;

  const { repoKey, onMessageSent, onError } = options;
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sendMessage = useCallback(async (
    message: string,
    mode: ChatMode
  ) => {
    if (!message.trim()) return;

    setIsLoading(true);
    setError(null);

    try {
      // Add user message immediately
      const userMessage: ChatMessage = {
        id: `user-${Date.now()}`,
        role: 'user',
        content: message,
        timestamp: new Date(),
        mode,
      };

      setMessages(prev => [...prev, userMessage]);

      // Send to API
      const request: ChatRequest = {
        repoKey,
        message,
        mode,
      };

      const response = await RepoChatApi.sendMessage(request);

      // Add assistant response
      setMessages(prev => [...prev, response.message]);

      onMessageSent?.(response.message);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to send message';
      setError(errorMessage);
      onError?.(err instanceof Error ? err : new Error(errorMessage));
    } finally {
      setIsLoading(false);
    }
  }, [repoKey, onMessageSent, onError]);

  const clearMessages = useCallback(() => {
    setMessages([]);
    setError(null);
  }, []);

  const loadHistory = useCallback(async () => {
    try {
      setIsLoading(true);
      const history = await RepoChatApi.getChatHistory(repoKey);
      setMessages(history);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load chat history';
      setError(errorMessage);
      onError?.(err instanceof Error ? err : new Error(errorMessage));
    } finally {
      setIsLoading(false);
    }
  }, [repoKey, onError]);

  return {
    messages,
    isLoading,
    error,
    sendMessage,
    clearMessages,
    loadHistory,
  };
};