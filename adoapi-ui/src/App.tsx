import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AppThemeProvider } from './theme';
import { Layout } from './components';
import { DocsPage, SettingsPage } from './pages';
import AuthProvider from './auth/AuthProvider';
import './App.css';

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
            </Routes>
          </Layout>
        </Router>
      </AppThemeProvider>
    </AuthProvider>
  );
}

export default App;
