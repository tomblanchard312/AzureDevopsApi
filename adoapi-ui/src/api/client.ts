import axios, { type AxiosInstance, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import React from 'react';
import { z } from 'zod';
import type {
  Project,
  Repository,
  DocsPreviewRequest,
  DocsPreviewResponse,
  DocsApplyRequest,
  DocsApplyResponse,
  PullRequestCommentRequest,
  PullRequestCommentResponse,
  InlineCommentRequest,
  InlineCommentResponse,
  ThreadResolutionRequest,
  ThreadResolutionResponse,
  PrStatusRequest,
  PrStatusResponse,
  ApiError,
} from '../types/api';
import {
  ProjectSchema,
  RepositorySchema,
  DocsPreviewResponseSchema,
  DocsApplyResponseSchema,
  PullRequestCommentResponseSchema,
  InlineCommentResponseSchema,
  ThreadResolutionResponseSchema,
  PrStatusResponseSchema,
} from '../types/api';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { apiTokenRequest } from '../auth/msalConfig';

// Generate correlation ID for request tracking
function generateCorrelationId(): string {
  return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
}

// Create axios instance with base configuration
const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.DEV ? '' : (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'),
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Global reference to MSAL instance (will be set by the hook)
let msalInstance: ReturnType<typeof useMsal> | null = null;

// Function to set MSAL instance (called from components that use the API)
export function setMsalInstance(instance: ReturnType<typeof useMsal>) {
  msalInstance = instance;
}

// Function to acquire token silently
async function acquireTokenSilent(): Promise<string | null> {
  if (!msalInstance) {
    throw new Error('MSAL instance not initialized. Make sure to call setMsalInstance() in a component that uses MSAL.');
  }

  const { instance, accounts } = msalInstance;
  const account = accounts[0];

  if (!account) {
    throw new Error('No authenticated account found. Please sign in first.');
  }

  try {
    const response = await instance.acquireTokenSilent({
      ...apiTokenRequest,
      account,
    });
    return response.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      // Token expired or requires interaction, try to acquire interactively
      try {
        const response = await instance.acquireTokenPopup(apiTokenRequest);
        return response.accessToken;
      } catch (popupError) {
        console.error('Failed to acquire token interactively:', popupError);
        throw new Error('Authentication required. Please sign in again.');
      }
    }
    console.error('Failed to acquire token silently:', error);
    throw new Error('Failed to acquire access token for API calls.');
  }
}

// Request interceptor to add correlation ID and authorization header
apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig): Promise<InternalAxiosRequestConfig> => {
    const correlationId = generateCorrelationId();
    config.headers['X-Correlation-ID'] = correlationId;

    // Skip token acquisition for non-API requests or if MSAL is not initialized
    if (!config.url?.startsWith('/api/') || !msalInstance) {
      return config;
    }

    try {
      const token = await acquireTokenSilent();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    } catch (error) {
      console.error('Failed to acquire token for request:', error);
      // Don't fail the request here, let it proceed without auth
      // The response interceptor will handle auth errors
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling and token retry
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const correlationId = error.config?.headers?.['X-Correlation-ID'] || 'unknown';
    const originalRequest = error.config;

    // Handle 401 Unauthorized - try to refresh token once
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        console.log('Token expired, attempting to acquire new token...');
        const token = await acquireTokenSilent();

        if (token) {
          // Update the authorization header
          originalRequest.headers.Authorization = `Bearer ${token}`;

          // Retry the original request
          return apiClient(originalRequest);
        }
      } catch (tokenError) {
        console.error('Failed to refresh token:', tokenError);
        // Fall through to error handling
      }
    }

    // Handle authentication-related errors with friendly messages
    if (error.response?.status === 401) {
      const apiError: ApiError = {
        message: 'Authentication failed. Please sign in again.',
        statusCode: 401,
        correlationId,
        details: {
          suggestion: 'Your session may have expired. Try refreshing the page or signing in again.',
          originalError: error.response?.data,
        },
      };
      return Promise.reject(apiError);
    }

    if (error.response?.status === 403) {
      const apiError: ApiError = {
        message: 'Access denied. You do not have permission to access this resource.',
        statusCode: 403,
        correlationId,
        details: {
          suggestion: 'Contact your administrator if you believe this is an error.',
          originalError: error.response?.data,
        },
      };
      return Promise.reject(apiError);
    }

    // Handle network errors
    if (!error.response) {
      const apiError: ApiError = {
        message: 'Network error. Please check your internet connection and try again.',
        statusCode: 0,
        correlationId,
        details: {
          suggestion: 'Check your network connection and try again.',
          originalError: error.message,
        },
      };
      return Promise.reject(apiError);
    }

    // Handle other API errors
    const apiError: ApiError = {
      message: error.response?.data?.message || error.message || 'An unexpected error occurred',
      statusCode: error.response?.status || 500,
      correlationId,
      details: error.response?.data,
    };

    return Promise.reject(apiError);
  }
);

// Generic function to validate response with zod schema
async function validateResponse<T>(
  response: AxiosResponse,
  schema: z.ZodSchema<T>
): Promise<T> {
  try {
    return schema.parse(response.data);
  } catch (error) {
    if (error instanceof z.ZodError) {
      const correlationId = response.config.headers['X-Correlation-ID'] || 'unknown';
      throw {
        message: 'Response validation failed',
        statusCode: 422,
        correlationId,
        details: {
          validationErrors: error.issues,
          receivedData: response.data,
        },
      } as ApiError;
    }
    throw error;
  }
}

// API functions
export const api = {
  // Get all projects
  async getProjects(): Promise<Project[]> {
    const response = await apiClient.get('/api/project/projects');
    return validateResponse(response, z.array(ProjectSchema));
  },

  // Get repositories for a specific project
  async getRepositories(project: string): Promise<Repository[]> {
    const response = await apiClient.get(`/api/repository/repositories?project=${encodeURIComponent(project)}`);
    return validateResponse(response, z.array(RepositorySchema));
  },

  // Preview documentation generation
  async previewDocs(request: DocsPreviewRequest): Promise<DocsPreviewResponse> {
    const response = await apiClient.post('/api/docs/preview', request);
    return validateResponse(response, DocsPreviewResponseSchema);
  },

  // Apply documentation changes
  async applyDocs(request: DocsApplyRequest): Promise<DocsApplyResponse> {
    const response = await apiClient.post('/api/docs/apply', request);
    return validateResponse(response, DocsApplyResponseSchema);
  },

  // PR comment methods
  async postPullRequestComment(request: PullRequestCommentRequest): Promise<PullRequestCommentResponse> {
    const response = await apiClient.post('/api/security-advisor/pr/comment', request);
    return validateResponse(response, PullRequestCommentResponseSchema);
  },

  async previewPullRequestComment(request: PullRequestCommentRequest): Promise<PullRequestCommentResponse> {
    const response = await apiClient.post('/api/security-advisor/pr/comment/preview', request);
    return validateResponse(response, PullRequestCommentResponseSchema);
  },

  async updatePullRequestComment(threadId: number, request: PullRequestCommentRequest): Promise<PullRequestCommentResponse> {
    const response = await apiClient.put(`/api/security-advisor/pr/comment/${threadId}`, request);
    return validateResponse(response, PullRequestCommentResponseSchema);
  },

  // Enhanced PR integration methods
  async postInlineComment(request: InlineCommentRequest): Promise<InlineCommentResponse> {
    const response = await apiClient.post('/api/security-advisor/pr/inline-comment', request);
    return validateResponse(response, InlineCommentResponseSchema);
  },

  async resolveFixedThreads(request: ThreadResolutionRequest): Promise<ThreadResolutionResponse> {
    const response = await apiClient.post('/api/security-advisor/pr/resolve-threads', request);
    return validateResponse(response, ThreadResolutionResponseSchema);
  },

  async postPrStatus(request: PrStatusRequest): Promise<PrStatusResponse> {
    const response = await apiClient.post('/api/security-advisor/pr/status', request);
    return validateResponse(response, PrStatusResponseSchema);
  },
};

// Export the axios instance for advanced usage if needed
export { apiClient };
export default api;

// React hook to initialize API client with MSAL
export function useApiClient() {
  const msal = useMsal();

  React.useEffect(() => {
    setMsalInstance(msal);
  }, [msal]);

  return api;
}