import { For, Index, JSX, Show, batch, createEffect, createMemo, createResource, createSignal, mapArray, on, onCleanup, onMount } from "solid-js";
import { DeleteMod, ILibrary, IMod, InstallModFromUrl, UpdateModState, UploadMod, getModsList } from "../../api/mods";
import defaultImage from "@/assets/DefaultCover.png"
import "./GetBeatSaberMods.scss";
import { modsList, mutateMods, refetchMods } from "../../state/mods";
import { CompareStringsAlphabetically, Sleep } from "../../util";
import toast from "solid-toast";
import { Title } from "@solidjs/meta";
import PageLayout from "../../Layouts/PageLayout";
import Box from "@suid/material/Box";
import RunButton from "../../components/Buttons/RunButton";
import { PlusIcon, UploadRounded } from "../../assets/Icons";
import PlayArrowRounded from '@suid/icons-material/PlayArrowRounded';
import { IconButton, List, ListItem, Switch, Typography } from "@suid/material";
import CloseRounded from "@suid/icons-material/CloseRounded";
import { FiInstagram, FiRefreshCcw } from "solid-icons/fi";
import { gotAccessToAppAndroidFolders, grantAccessToAppAndroidFolders, launchCurrentApp } from "../../api/android";
import { config, currentApplication, moddingStatus, patchingOptions, refetchModdingStatus, refetchSettings } from "../../store";
import { showChangeGameModal } from "../../modals/ChangeGameModal";
import { getPatchedModdingStatus } from "../../api/patching";
import { proxyFetch } from "../../api/app";
import { ModDropper } from "../../components/ModDropper";
import { ModEntry, ModRawEntry, ModVersion, ParseModVersions } from "./GetBeatSaberUtils";
import { createStore } from "solid-js/store";
import { FaSolidDownload } from "solid-icons/fa";
import { gt } from "semver";


const [isModdableVersion, setIsModdableVersion] = createSignal(false);

interface CoreModInfo {
    id: string,
    version: string,
    downloadLink: string
}

interface ModsJsonState {
    mods: ModEntry[],
    coreMods: {
        lastUpdated: string, mods: Array<CoreModInfo>
    }
}

const [modIndex, setModIndex] = createStore<ModsJsonState>({
    mods: [],
    coreMods: {
        lastUpdated: "",
        mods: []
    }
});

async function refetchModListForVersion() {
    let mods: ModEntry[] = []
    let coreMods: any;

    let version = moddingStatus()?.version ?? null;
    if (version == null) {
        setIsModdableVersion(false);
        return;
    };

    {
        let text = await proxyFetch("https://computerelite.github.io/tools/Beat_Saber/coreMods.json");
        let json = JSON.parse(text);

        if (json[version] == null) {
            setIsModdableVersion(false);
            return;
        }

        coreMods = json[version];
    }


    {
        let text = await proxyFetch("https://computerelite.github.io/tools/Beat_Saber/mods.json");
        let json = JSON.parse(text);

        // check if json has version
        if (json[version]) {
            mods = ParseModVersions(json[version]);
        } else {
            mods = [];
        }
    }

    // Sort mods alphabetically
    mods = mods.sort((a, b) => CompareStringsAlphabetically(a.name, b.name));
    coreMods.mods = coreMods.mods.sort((a: CoreModInfo, b: CoreModInfo) => CompareStringsAlphabetically(a.id, b.id));

    setModIndex({
        mods,
        coreMods: coreMods
    });
    console.log(modIndex)
}

async function installCoreMods() {
    let moddedStatus = moddingStatus();
    if (moddedStatus == null) return;

    if (moddedStatus.isInstalled == false) {
        return toast.error("Game is not installed! Install it first before installing mods");
    }

    if (moddedStatus.isPatched == false) {
        return toast.error("Game is not modded! Mod it first before installing mods");
    }

    if (moddedStatus.version == null) {
        return toast.error("Game version is unknown! Try to reload QAVS, something is wrong");
    }

    await InstallModFromUrl(`https://oculusdb.rui2015.me/api/coremodsdownload/${moddedStatus.version}.qmod`);
    toast.success("Install of core mods started! Check the status in the mods page");
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

export default function GetBeatSabersModsPage() {

    // Remember last scroll position
    onMount(() => {
        // SCROLL TO LAST POSITION
        window.scrollTo(0, lastScrollPosition);
    })
    onCleanup(() => {
        lastScrollPosition = window.scrollY;
    });


    createEffect(on([moddingStatus], async () => {
        let version = moddingStatus()?.version ?? null;
        console.log(moddingStatus())
        if (version != null) {
            await refetchModListForVersion();
        }
    }));


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
                        <RunButton text='Install core mods' variant="info" hideTextOnMobile icon={<FiInstagram />} onClick={installCoreMods} />
                    </Box>
                    <Box sx={{
                        display: "flex",
                        gap: 2,
                        alignItems: "center",
                    }}>
                        <RunButton icon={<FiRefreshCcw />} onClick={reloadMods} />
                        <RunButton text='Delete all' onClick={() => { }} style={"width: 80px"} />
                    </Box>
                </Box>

                {/* <h2>Cores</h2>
                <List sx={{
                    display: "flex",
                    flexDirection: "column",
                    gap: 1,
                }}>
                    <Index each={modIndex?.coreMods.mods} fallback={<div class="emptyText">No mods</div>}>
                        {(mod) => (
                            <CoreModCard mod={mod()} />
                        )}
                    </Index>
                </List> */}
                <h2>Mods</h2>
                <List sx={{
                    display: "flex",
                    flexDirection: "column",
                    gap: 1,
                }}>
                    <For each={modIndex?.mods} fallback={<div>Emptiness..</div>}  >
                        {(mod) => (
                            <ModCoverLessCard mod={mod} />
                        )}
                    </For>
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

function ModCoverLessCard({ mod }: { mod: ModEntry }) {
    let modStatus = createMemo(() => {
        // @ts-ignore
        let status: {
            existingMod: ILibrary | null,
            isInstalled: boolean,
            isEnabled: boolean,
            latestVersion: ModVersion | undefined,
            hasUpdate: boolean,
        } = {};
        
        let existingMod = modsList()?.find(x => x.Id == mod.id);
        if (existingMod) {
            status.isEnabled = existingMod.IsInstalled;
            status.isInstalled = true;
            status.existingMod = existingMod;
        }

        let latestVersion = (mod && mod.versions && mod.versions.length > 0)? mod.versions[0]: undefined;

        // Check if there is an update to the mod
        if (latestVersion && existingMod) {
            if (gt(latestVersion.version,existingMod.VersionString)) {
                status.hasUpdate = true;
            } else {
                status.hasUpdate = false;
            }
        } else {
            status.hasUpdate = false;
        }

        
        return status;

    });

    return (
        <ListItem class="mod" sx={{
            display: "flex",
            width: "100%",
            backgroundColor: "#111827",
            borderRadius: "6px"
        }}>
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


                    }}  >{mod.name}</Typography>
                    <Typography variant="caption" sx={{

                        fontFamily: 'Roboto',
                        fontStyle: 'normal',
                        fontWeight: 400,
                        fontSize: '10px',
                        lineHeight: '12px',
                    }} class="text-accent"  >v{mod.versions[0].version} {mod.author}</Typography>
                </Box>
                <Typography sx={{
                    fontFamily: 'Roboto',
                    fontStyle: 'normal',
                    fontWeight: 400,
                    fontSize: '10px',
                    lineHeight: '12px',
                    color: '#D1D5DB',
                    marginTop: 1,
                }}>{mod.description}</Typography>
            </Box>
            <Box sx={{
                display: "flex",
                flexDirection: "column",
                justifyContent: "space-between",
            }}>
                <Show when={modStatus().isInstalled}>
                    <RunButton onClick={async () => {
                        let existingMod = modStatus()!.existingMod as ILibrary;
                        DeleteModClick(existingMod);
                    }} icon={<CloseRounded />}/>
                </Show>

                <Show when={!modStatus().isInstalled || modStatus().hasUpdate}>
                    <RunButton icon={<FaSolidDownload />} onClick={
                        () => InstallModFromUrl(mod.versions[0].download)
                    } />
                </Show>

            </Box>
        </ListItem>
    )
}