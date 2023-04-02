import { Component, createEffect, createSignal } from 'solid-js';

import logo from './logo.svg';
import './legacy.css';
import Sidebar from './components/Sidebar';
import { Routes, Route, Router, hashIntegration, Navigate } from "@solidjs/router";
import BackupPage from './pages/BackupPage';
import DowngradePage from './pages/DowngradePage';
import DownloadProgressPage from './pages/DownloadProgressPage';
import GetModsPage from './pages/GetModsPage';
import CosmeticsPage from './pages/CosmeticsPage';
import ModsPage from './pages/ModsPage';
import PatchingPage from './pages/PatchingPage';
import ToolsPage from './pages/ToolsPage';


const App: Component = () => {
  let [count, setCount] = createSignal(0);

  createEffect(() => {
    console.log('count', count());

    fetch('http://localhost:50002/api/mods/mods')
  });

  return (
    <Router source={hashIntegration()} >
      <div class='menuContainer'>
        <Sidebar />
        <div class="content">
          <div class="contentItem">
            <Routes>
              <Route path="/" element={<Navigate href={"/backup"} />} />
              <Route path="/backup" element={<BackupPage />} />
              <Route path="/downgrade" element={<DowngradePage />} />
              <Route path="/downloads" element={<DownloadProgressPage />} />
              <Route path="/patching" element={<PatchingPage />} />
              <Route path="/mods" element={<ModsPage />} />
              <Route path="/cosmetics" element={<CosmeticsPage />} />
              <Route path="/getMods" element={<GetModsPage />} />
              <Route path="/tools" element={<ToolsPage />} />
            </Routes>
          </div>
        </div>
      </div>
    </Router>

  );
};

export default App;
