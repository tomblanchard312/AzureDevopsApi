import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AppThemeProvider } from './theme';
import { Layout } from './components';
import { DocsPage, SettingsPage } from './pages';
import { RepoOverviewPage, RepoMemoryPage, RepoInsightsPage, RepoWorkItemsPage, RepoAgentRunsPage, RepoAutomationPolicyPage } from './pages';
import AuthProvider from './auth/AuthProvider';
import './App.css';

// TODO: Import and mount RepoChatPanel here for repository-aware chat functionality
// import { RepoChatPanel } from './features/repo-chat';

function App() {
  return (
    <AuthProvider>
      <AppThemeProvider>
        <Router>
          <Layout>
            <Routes>
              <Route path="/" element={<Navigate to="/docs" replace />} />
              <Route path="/docs" element={<DocsPage />} />
              <Route path="/settings" element={<SettingsPage />} />
              <Route path="/repos/:repoKey/overview" element={<RepoOverviewPage />} />
              <Route path="/repos/:repoKey/memory" element={<RepoMemoryPage />} />
              <Route path="/repos/:repoKey/insights" element={<RepoInsightsPage />} />
              <Route path="/repos/:repoKey/work-items" element={<RepoWorkItemsPage />} />
              <Route path="/repos/:repoKey/agent-runs" element={<RepoAgentRunsPage />} />
              <Route path="/repos/:repoKey/automation-policy" element={<RepoAutomationPolicyPage />} />
            </Routes>
            {/* TODO: Mount RepoChatPanel here to enable repository-aware chat across all pages */}
            {/* <RepoChatPanel /> */}
          </Layout>
        </Router>
      </AppThemeProvider>
    </AuthProvider>
  );
}

export default App;
