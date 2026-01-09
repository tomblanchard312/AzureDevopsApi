import React, { useState, useEffect } from 'react';
import { useApiClient } from './api';
import type { Project, Repository } from './types';

const ApiDemo: React.FC = () => {
  const api = useApiClient();
  const [projects, setProjects] = useState<Project[]>([]);
  const [repositories, setRepositories] = useState<Repository[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchProjects = React.useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await api.getProjects();
      setProjects(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch projects');
    } finally {
      setLoading(false);
    }
  }, [api]);

  const fetchRepositories = async (projectName: string) => {
    setLoading(true);
    setError(null);
    try {
      const result = await api.getRepositories(projectName);
      setRepositories(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch repositories');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProjects();
  }, [fetchProjects]);

  return (
    <div style={{ padding: '20px' }}>
      <h1>Azure DevOps API Demo</h1>

      {error && (
        <div style={{ color: 'red', marginBottom: '10px' }}>
          Error: {error}
        </div>
      )}

      <div style={{ marginBottom: '20px' }}>
        <h2>Projects</h2>
        <button onClick={fetchProjects} disabled={loading}>
          {loading ? 'Loading...' : 'Refresh Projects'}
        </button>
        <ul>
          {projects.map((project) => (
            <li key={project.id}>
              <strong>{project.name}</strong> - {project.description}
              <button
                onClick={() => fetchRepositories(project.name)}
                style={{ marginLeft: '10px' }}
                disabled={loading}
              >
                Load Repositories
              </button>
            </li>
          ))}
        </ul>
      </div>

      {repositories.length > 0 && (
        <div>
          <h2>Repositories</h2>
          <ul>
            {repositories.map((repo) => (
              <li key={repo.id}>
                <strong>{repo.name}</strong> - {repo.url}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default ApiDemo;