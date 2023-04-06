import { Component, createEffect, createSignal } from 'solid-js';
import toast, { Toaster } from 'solid-toast';

import "normalize.css"
import "./global.scss";
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
import { MetaProvider } from '@solidjs/meta';
import ModalContainer from './modals/ModalContainer';

// Fonts
import "@fontsource/roboto"; // Defaults to weight 400.
import "@fontsource/roboto/300.css"; // Weight 300.
import "@fontsource/roboto/500.css"; // Weight 500.
import "@fontsource/roboto/700.css"; // Weight 700.


import style from "./App.module.scss"
import { ThemeProvider } from '@suid/material';
import { theme } from './theme';


const App: Component = () => {
  return (
    <MetaProvider>
     <ThemeProvider theme={theme}>
      <Router source={hashIntegration()} >
        <div class={style['AppRoot']}>
          <Sidebar />
          <div class={style.content}>
            <Routes>
              <Route path="/" element={<Navigate href={"/backup"} />} />
              <Route path="/backup" element={<BackupPage />} />
              <Route path="/downgrade" element={<DowngradePage />} />
              <Route path="/downloads" element={<DownloadProgressPage />} />
              <Route path="/patching" element={<PatchingPage />} />
              <Route path="/mods" element={<ModsPage />} />
              <Route path="/cosmetics" element={<CosmeticsPage />} />
              {/* <Route path="/getMods" element={<GetModsPage />} /> */}
              <Route path="/tools" element={<ToolsPage />} />
            </Routes>
          </div>
        </div>
        <Toaster
          gutter={8}
          position="bottom-left"
        />
        <ModalContainer/>
      </Router>
      </ThemeProvider>
    </MetaProvider>
  );
};

export default App;
