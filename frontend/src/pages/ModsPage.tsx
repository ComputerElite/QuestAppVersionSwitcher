import { For, Index, JSX, Show, batch, createEffect, createMemo, createSignal, mapArray, onCleanup, onMount } from "solid-js";
import { DeleteMod, IMod, InstallModFromUrl, UpdateModState, UploadMod, getModsList } from "../api/mods";
import defaultImage from "./../assets/DefaultCover.png"
import "./ModsPage.scss";
import { modsList, mutateMods, refetchMods } from "../state/mods";
import { CompareStringsAlphabetically, Sleep } from "../util";
import toast from "solid-toast";
import { Title } from "@solidjs/meta";
import PageLayout from "../Layouts/PageLayout";
import Box from "@suid/material/Box";
import RunButton from "../components/Buttons/RunButton";
import { PlusIcon, UploadRounded } from "../assets/Icons";
import PlayArrowRounded from '@suid/icons-material/PlayArrowRounded';
import { IconButton, List, ListItem, Switch, Typography } from "@suid/material";
import CloseRounded from "@suid/icons-material/CloseRounded";
import { FiRefreshCcw } from "solid-icons/fi";
import { gotAccessToAppAndroidFolders, grantAccessToAppAndroidFolders, launchCurrentApp } from "../api/android";
import { config, currentApplication, moddingStatus, patchingOptions, refetchModdingStatus, refetchSettings } from "../store";
import { showChangeGameModal } from "../modals/ChangeGameModal";
import { getPatchedModdingStatus } from "../api/patching";
import { proxyFetch } from "../api/app";
import { ModDropper } from "../components/ModDropper";

async function UploadModClick() {

  if (!(await checkModsCanBeInstalled())) return;

  var input = document.createElement('input');
  input.type = 'file';
  input.multiple = true;
  input.click();
  input.addEventListener("change", async function (e) {

    if (this.files && this.files.length > 0) {
      for (const file of this.files) {
        await UploadMod(file);
        
      }
    }
  })
}

async function checkModsCanBeInstalled() {
  if (config()?.currentApp == null) {
    showChangeGameModal();
    toast.error("No game selected! Select a game first");
    return false;
  }

  if (!(moddingStatus()?.isInstalled ?? false)) {
    return toast.error("Game is not installed! Install it first before installing mods");
  }

  if (!(moddingStatus()?.isPatched ?? false)) {
    return toast.error("Game is not modded! Mod it first before installing mods");
  }

  let hasAccess = await gotAccessToAppAndroidFolders(config()!.currentApp);
  if (!hasAccess) {
    toast.error("Failed to get access to game folders. We will request access again in 3 seconds, try again after that.");
    Sleep(3000);
    let result = await grantAccessToAppAndroidFolders(config()!.currentApp);
    return false;
  }

  return true;
}

let lastScrollPosition = 0;

export default function ModsPage() {

  // Remember last scroll position
  onMount(() => {
    // SCROLL TO LAST POSITION
    window.scrollTo(0, lastScrollPosition);
  })
  onCleanup(() => {
    lastScrollPosition = window.scrollY;
  });

  async function reloadMods() {
    try {
      await refetchMods();
      toast.success("Mods reloaded");
    } catch (error) {
      console.error(error);
      toast.error("Failed to reload mods");
    }
  }

  async function onGameStart() {
    await refetchSettings();
    
    if (!config()?.currentApp) {
      toast.error("Please select a game first");
      return await showChangeGameModal();
    }

    await refetchModdingStatus();
    if (!(moddingStatus()?.isInstalled ?? false)) {
      return toast.error("Game is not installed");
    }

    await launchCurrentApp();
    toast.success("Game started");
  }

  let filteredData = createMemo<{ mods?: Array<IMod>, libs?: Array<IMod> }>(() => {
    let allMods = modsList();
    if (!allMods) return { mods: [], libs: [] };

    // Deduplicate mods cause QAVS is dumb sometimes
    let ids = new Set<string>();
    allMods = allMods.filter((m) => {
      if (ids.has(m.Id)) return false;
      ids.add(m.Id);
      return true;
    });

    return {
      mods: allMods?.filter((s) => !s.IsLibrary).sort((a, b) => CompareStringsAlphabetically(a.Name, b.Name)),
      libs: allMods?.filter((s) => s.IsLibrary).sort((a, b) => CompareStringsAlphabetically(a.Name, b.Name))
    }
  }
  );

  return (
    <PageLayout>
      <ModDropper/>
      <div
        class=" contentItem modsPage"
      >
        <Box sx={{
          display: "flex",
          width: "100%",
          gap: 1,
          flexWrap: "wrap",
          justifyContent: "space-between",
          marginBottom: 2,
        }}>
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <RunButton text='Run the app' variant="success" hideTextOnMobile icon={<PlayArrowRounded />} onClick={onGameStart} />
            <RunButton text='Upload a mod' icon={<UploadRounded />} hideTextOnMobile onClick={UploadModClick} /> 
          </Box>
          <Box sx={{
            display: "flex",
            gap: 2,
            alignItems: "center",
          }}>
            <span style={{
              "font-family": "Roboto",
              "font-style": "normal",
              "font-weight": "400",
              "font-size": "12px",
              "line-height": "14px",
              "display": "flex",
              "align-items": "center",
              "text-align": "center",
              "color": "#D1D5DB",
              "margin-left": "10px",
            }} class="text-accent" >
              Get more mods
            </span>
            <RunButton icon={<FiRefreshCcw />} onClick={reloadMods} />
            {/* <RunButton text='Delete all' onClick={() => { }} style={"width: 80px"} /> */}
          </Box>
        </Box>
        <List sx={{
          display: "flex",
          flexDirection: "column",
          gap: 1,
        }}>
          <For each={filteredData().mods} fallback={<div>Emptiness..</div>}  >
            {(mod) => (
              <ModCard mod={mod} />
            )}
          </For>
        </List>
        <h2>Installed Libraries</h2>
        <List sx={{
          display: "flex",
          flexDirection: "column",
          gap: 1,
        }}>
          <Index each={filteredData()?.libs} fallback={<div class="emptyText">No mods</div>}>
            {(mod) => (
              <ModCard mod={mod()} />
            )}
          </Index>
        </List>
      </div>
    </PageLayout>
  )
}

async function ToggleModState(modId: string, newState: boolean) {
  await UpdateModState(modId, newState);
  // await Sleep(300);
  // refetchMods();
  // toast.success(`Mod ${modId} is ${newState ? "enabled" : "disabled"}`)
}

async function DeleteModClick(mod: IMod) {
  await DeleteMod(mod.Id);
  await Sleep(300);
  refetchMods();
  toast.success(`Mod ${mod.Name} is deleted`)
}

function ModCard({ mod }: { mod: IMod }) {
  return (
    <ListItem class="mod" sx={{
      display: "flex",
      width: "100%",
      backgroundColor: "#111827",
      borderRadius: "6px"
    }}>
      <img class="modCover" style={{
        "margin-right": "14px",
        "border-radius": "6px",
        "object-fit": "cover",

        "width": "160px",
        "height": "92px",
        "aspect-ratio": "16 / 9",
      }} src={(mod.hasCover) ? `/api/mods/cover/${mod.Id}` : defaultImage} />
      <Box
        sx={{
          flexGrow: 1,
        }}
      >
        <Box sx={{
          display: "flex",
          alignItems: "center",
          flexWrap: "wrap",
        }}>
          <Typography variant="h6" sx={{
            fontFamily: 'Roboto',
            fontStyle: 'normal',
            fontWeight: 400,
            fontSize: '16px',
            lineHeight: '19px',
            color: '#FFFFFF',
            marginRight: 1,


          }}  >{mod.Name}</Typography>
          <Typography variant="caption" sx={{

            fontFamily: 'Roboto',
            fontStyle: 'normal',
            fontWeight: 400,
            fontSize: '10px',
            lineHeight: '12px',
          }} class="text-accent"  >v{mod.VersionString} {mod.Author ? `by ${mod.Author}` : ""}</Typography>
        </Box>
        <Typography sx={{
          fontFamily: 'Roboto',
          fontStyle: 'normal',
          fontWeight: 400,
          fontSize: '10px',
          lineHeight: '12px',
          color: '#D1D5DB',
          marginTop: 1,
        }}>{mod.Description}</Typography>
      </Box>
      <Box sx={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
      }}>
        <IconButton onClick={() => DeleteModClick(mod)}>
          <CloseRounded />
        </IconButton>
        <Switch checked={mod.IsInstalled} onChange={() => ToggleModState(mod.Id, !mod.IsInstalled)} />
      </Box>
    </ListItem>
  )
}
