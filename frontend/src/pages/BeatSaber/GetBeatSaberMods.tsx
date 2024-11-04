import {
  For,
  Index,
  JSX,
  Show,
  batch,
  createEffect,
  createMemo,
  createResource,
  createSignal,
  mapArray,
  on,
  onCleanup,
  onMount,
} from "solid-js";
import {
  DeleteMod,
  ILibrary,
  IMod,
  InstallModFromUrl,
  UpdateModState,
  UploadMod,
  getModsList,
} from "../../api/mods";
import defaultImage from "@/assets/DefaultCover.png";
import "./GetBeatSaberMods.scss";
import {
  BSCoreModInfo,
  BSCoreModsRaw,
  BSCoreModsVersionRaw,
  GetBeatsaberModdableVersions,
  beatSaberCores,
  getCoreModsList,
  modsList,
  mutateMods,
  refetchBeatSaberCores,
  refetchMods,
} from "../../state/mods";
import { CompareStringsAlphabetically, Sleep } from "../../util";
import toast from "solid-toast";
import { Title } from "@solidjs/meta";
import PageLayout from "../../Layouts/PageLayout";
import Box from "@suid/material/Box";
import RunButton from "../../components/Buttons/RunButton";
import { PlusIcon, UploadRounded } from "../../assets/Icons";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { IconButton, List, ListItem, Switch, Typography } from "@suid/material";
import CloseRounded from "@suid/icons-material/CloseRounded";
import DownloadRounded from "@suid/icons-material/DownloadRounded";
import { FiInstagram, FiRefreshCcw, FiTrash } from "solid-icons/fi";
import {
  gotAccessToAppAndroidFolders,
  grantAccessToAppAndroidFolders,
  launchCurrentApp,
} from "../../api/android";
import {
  config,
  currentApplication,
  moddingStatus,
  patchingOptions,
  refetchModdingStatus,
  refetchSettings,
} from "../../store";
import { showChangeGameModal } from "../../modals/ChangeGameModal";
import { getPatchedModdingStatus } from "../../api/patching";
import { proxyFetch } from "../../api/app";
import { ModDropper } from "../../components/ModDropper";
import {
  ModEntry,
  ModRawEntry,
  ModVersion,
  ParseModVersions,
} from "./GetBeatSaberUtils";
import { createStore } from "solid-js/store";
import { FaSolidDownload, FaSolidGear } from "solid-icons/fa";
import { gt } from "semver";
import { showConfirmModal } from "../../modals/ConfirmModal";

const [isModdableVersion, setIsModdableVersion] = createSignal(false);

interface ModsJsonState {
  mods: ModEntry[];
  coreMods: BSCoreModsVersionRaw;
}

const [modIndex, setModIndex] = createStore<ModsJsonState>({
  mods: [],
  coreMods: {
    lastUpdated: "",
    mods: [],
  },
});

async function refetchModListForVersion() {
  let mods: ModEntry[] = [];
  let coreMods: any;

  let version = moddingStatus()?.version ?? null;
  if (version == null) {
    setIsModdableVersion(false);
    return;
  }

  {
    let coreModsList = await getCoreModsList();

    if (!coreModsList) {
      setIsModdableVersion(false);
      return;
    }

    if (coreModsList[version] == null) {
      setIsModdableVersion(false);
      return;
    }
    coreMods = coreModsList[version];
  }

  {
    let json: any;

    try {
      const response = await proxyFetch(
        "https://computerelite.github.io/tools/Beat_Saber/mods.json",
      );
      if (!response.ok) {
        toast.error("Failed to fetch mod list");
        return;
      }
      json = await response.json();
    } catch (error) {
      toast.error("Failed to fetch mod list");
      return;
    }

    // check if json has version
    if (json[version]) {
      mods = ParseModVersions(json[version]);
    } else {
      mods = [];
    }
  }

  // Sort mods alphabetically
  mods = mods.sort((a, b) => CompareStringsAlphabetically(a.name, b.name));
  coreMods.mods = coreMods.mods.sort((a: BSCoreModInfo, b: BSCoreModInfo) =>
    CompareStringsAlphabetically(a.id, b.id),
  );

  setModIndex({
    mods,
    coreMods: coreMods,
  });
  console.log(modIndex);
}

async function installCoreMods() {
  let moddedStatus = moddingStatus();
  if (moddedStatus == null) return;

  if (moddedStatus.isInstalled == false) {
    return toast.error(
      "Game is not installed! Install it first before installing mods",
    );
  }

  if (moddedStatus.isPatched == false) {
    return toast.error(
      "Game is not modded! Mod it first before installing mods",
    );
  }

  if (moddedStatus.version == null) {
    return toast.error(
      "Game version is unknown! Try to reload QAVS, something is wrong",
    );
  }

  await InstallModFromUrl(
    `https://oculusdb.rui2015.me/api/coremodsdownload/${moddedStatus.version}.qmod`,
  );
  toast.success(
    "Install of core mods started! Check the status in the mods page",
  );
}

async function checkModsCanBeInstalled() {
  if (config()?.currentApp == null) {
    showChangeGameModal();
    toast.error("No game selected! Select a game first");
    return false;
  }

  if (!(moddingStatus()?.isInstalled ?? false)) {
    return toast.error(
      "Game is not installed! Install it first before installing mods",
    );
  }

  if (!(moddingStatus()?.isPatched ?? false)) {
    return toast.error(
      "Game is not modded! Mod it first before installing mods",
    );
  }

  let hasAccess = await gotAccessToAppAndroidFolders(config()!.currentApp);
  if (!hasAccess) {
    toast.error(
      "Failed to get access to game folders. We will request access again in 3 seconds, try again after that.",
    );
    Sleep(3000);
    let result = await grantAccessToAppAndroidFolders(config()!.currentApp);
    return false;
  }

  return true;
}

let lastScrollPosition = 0;

export default function GetBeatSabersModsPage() {
  // Remember last scroll position
  onMount(() => {
    // SCROLL TO LAST POSITION
    window.scrollTo(0, lastScrollPosition);
  });
  onCleanup(() => {
    lastScrollPosition = window.scrollY;
  });

  createEffect(
    on([moddingStatus], async () => {
      let version = moddingStatus()?.version ?? null;
      console.log(moddingStatus());
      if (version != null) {
        await refetchModListForVersion();
      }
    }),
  );

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

  return (
    <PageLayout>
      <ModDropper />
      <div class=" contentItem modsPage">
        <Box
          sx={{
            display: "flex",
            width: "100%",
            gap: 1,
            flexWrap: "wrap",
            justifyContent: "space-between",
            marginBottom: 2,
          }}
        >
          <Box
            sx={{
              display: "flex",
              gap: 2,
              alignItems: "center",
            }}
          >
            <RunButton
              text="Run the app"
              variant="success"
              hideTextOnMobile
              icon={<PlayArrowRounded />}
              onClick={onGameStart}
            />
            <RunButton
              text="Install core mods"
              variant="info"
              hideTextOnMobile
              icon={<DownloadRounded />}
              onClick={installCoreMods}
            />
          </Box>
          <Box
            sx={{
              display: "flex",
              gap: 2,
              alignItems: "center",
            }}
          >
            <RunButton icon={<FiRefreshCcw />} onClick={reloadMods} />
            <RunButton
              variant="error"
              text="Delete all"
              icon={<FiTrash />}
              onClick={() => {}}
              style={"width: 80px"}
            />
          </Box>
        </Box>

        <h2>Mods</h2>
        <List
          sx={{
            flexDirection: "column",
            gap: 1,
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(300px, 1fr))",
          }}
        >
          <For each={modIndex?.mods} fallback={<div>Emptiness..</div>}>
            {(mod) => <ModCoverLessCard mod={mod} />}
          </For>
        </List>
      </div>
    </PageLayout>
  );
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
  toast.success(`Mod ${mod.Name} is deleted`);
}

function ModCoverLessCard(props: { mod: ModEntry }) {
  let modStatus = createMemo(() => {
    // @ts-ignore
    let status: {
      existingMod: ILibrary | null;
      isInstalled: boolean;
      isEnabled: boolean;
      latestVersion: ModVersion | undefined;
      hasUpdate: boolean;
    } = {};

    let existingMod = modsList()?.find((x) => x.Id == props.mod.id);
    if (existingMod) {
      status.isEnabled = existingMod.IsInstalled;
      status.isInstalled = true;
      status.existingMod = existingMod;
    }

    // Mods list is presorted
    let latestVersion =
      props.mod && props.mod.versions && props.mod.versions.length > 0
        ? props.mod.versions[0]
        : undefined;

    // Check if there is an update to the mod
    if (latestVersion && existingMod) {
      if (gt(latestVersion.version, existingMod.VersionString)) {
        status.hasUpdate = true;
        console.log("has update", props.mod.name);
      } else {
        status.hasUpdate = false;
      }
    } else {
      status.hasUpdate = false;
    }

    return status;
  });
  return (
    <ListItem
      class="mod"
      sx={{
        display: "flex",
        width: "100%",
        backgroundColor: "#111827",
        borderRadius: "6px",
        flexDirection: "column",
        alignItems: "left",
        padding: 0,
        overflow: "hidden",
      }}
    >
      {/* <div class="w-full">
                <Show when={props.mod.cover}>
                    <img class="aspect-video w-full object-cover" src={props.mod.cover!} alt={props.mod.name} />
                </Show>
                <Show when={!props.mod.cover}>
                    <img class="aspect-video w-full object-cover" src={defaultImage} alt={props.mod.name} />
                </Show>
            </div> */}
      <div class=" sm:p-3 lg:p-4">
        <Box
          sx={{
            flexGrow: 1,
          }}
        >
          <Box
            sx={{
              display: "flex",
              alignItems: "left",
              flexWrap: "wrap",
              flexDirection: "column",
            }}
          >
            <Typography
              variant="h6"
              sx={{
                fontFamily: "Roboto",
                fontStyle: "normal",
                fontWeight: 400,
                fontSize: "16px",
                lineHeight: "19px",
                color: "#FFFFFF",
                marginRight: 1,
              }}
            >
              {props.mod.name}
            </Typography>
            <Typography
              variant="caption"
              sx={{
                fontFamily: "Roboto",
                fontStyle: "normal",
                fontWeight: 400,
                fontSize: "10px",
                lineHeight: "12px",
              }}
              class="text-accent"
            >
              v{props.mod.versions[0].version} {props.mod.author}
            </Typography>
          </Box>

          <Typography
            sx={{
              fontFamily: "Roboto",
              fontStyle: "normal",
              fontWeight: 400,
              fontSize: "10px",
              lineHeight: "12px",
              color: "#D1D5DB",
              marginTop: 1,
            }}
          >
            {props.mod.description}
          </Typography>
        </Box>
        <Box
          sx={{
            display: "flex",
            flexDirection: "row",
            mt: 1,
            justifyContent: "space-between",
            gap: 1,
          }}
        >
          <Box
            sx={{
              flexGrow: 1,
              display: "flex",
            }}
          >
            <Show when={!modStatus().isInstalled}>
              <RunButton
                fullWidth
                text="Install"
                variant="success"
                icon={<FaSolidDownload />}
                onClick={() =>
                  InstallModFromUrl(props.mod.versions[0].download)
                }
              />
            </Show>
            <Show when={modStatus().hasUpdate}>
              <RunButton
                fullWidth
                text="Update"
                variant="info"
                icon={<FaSolidDownload />}
                onClick={() =>
                  InstallModFromUrl(props.mod.versions[0].download)
                }
              />
            </Show>
            <Show when={modStatus().isInstalled && !modStatus().hasUpdate}>
              <RunButton fullWidth text="Installed" disabled />
            </Show>
          </Box>

          <RunButton
            onClick={async () => {
              showConfirmModal({
                title: "This is a placeholder",
                message: "This is a placeholder",
                cancelText: "Cancel",
                okText: "Ok",
              });
            }}
            icon={<FaSolidGear />}
          />

          <Show when={modStatus().isInstalled}>
            <RunButton
              variant="error"
              onClick={async () => {
                let existingMod = modStatus()!.existingMod as ILibrary;
                DeleteModClick(existingMod);
              }}
              icon={<FiTrash />}
            />
          </Show>
        </Box>
      </div>
    </ListItem>
  );
}
