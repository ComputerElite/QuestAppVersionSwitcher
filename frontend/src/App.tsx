import { Component, createEffect, createSignal } from 'solid-js';
import toast, { Toaster } from 'solid-toast';

import "normalize.css"
import "./global.scss";
import Sidebar from './components/Sidebar';
import { Route, HashRouter , Navigate } from "@solidjs/router";
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


import style from "./App.module.scss"
import { ThemeProvider } from '@suid/material';
import { theme } from './theme';
import { refetchAppInfo, refetchCosmeticTypes, refetchModdingStatus } from './store';


// Font roboto
import '@fontsource/roboto';
import { InitWS } from './state/eventBus';
import GetBeatSabersModsPage from './pages/BeatSaber/GetBeatSaberMods';

const Root: Component = (props: any) => {
  // Load app info on startup
  createEffect(async () => {
    await refetchAppInfo();
    await refetchModdingStatus();
    await refetchCosmeticTypes();
  })

  return (
    <MetaProvider>
     <ThemeProvider theme={theme}>
  
        <div class={style['AppRoot']}>
          <Sidebar />
          <div class={style.content}>
            {/* <Routes> */}
              {props.children}
            {/* </Routes> */}
          </div>
        </div>
        <Toaster
          gutter={8}
          position="bottom-left"
        />
        <ModalContainer/>
      </ThemeProvider>
    </MetaProvider>
  );
};

const App = () => (
  <HashRouter root={Root}>
    <Route path="/" component={() => <Navigate href={"/backup"} />} />;
    <Route path="/backup" component={BackupPage} />
    <Route path="/downgrade" component={DowngradePage} />
    <Route path="/downloads" component={DownloadProgressPage} />
    <Route path="/patching" component={PatchingPage} />
    <Route path="/mods" component={ModsPage} />
    <Route path="/cosmetics" component={CosmeticsPage} />
    {/* <Route path="/getMods" component={<GetModsPage />} /> */}
    <Route path="/tools" component={ToolsPage} />
    <Route path="/bsmods" component={GetBeatSabersModsPage} />
  </HashRouter>
)

export default App;
