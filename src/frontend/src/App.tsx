import { Routes, Route } from 'react-router-dom';
import Shell from './components/layout/AppShell';
import ChatPage from './pages/ChatPage';
import EntitiesPage from './pages/EntitiesPage';
import ImportPage from './pages/ImportPage';
import ProposalsPage from './pages/ProposalsPage';

export default function App() {
  return (
    <Routes>
      <Route element={<Shell />}>
        <Route path="/" element={<ChatPage />} />
        <Route path="/entities" element={<EntitiesPage />} />
        <Route path="/import" element={<ImportPage />} />
        <Route path="/proposals" element={<ProposalsPage />} />
      </Route>
    </Routes>
  );
}
