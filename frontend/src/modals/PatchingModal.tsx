import { Show, createMemo, createSignal, onCleanup } from "solid-js";
import { BackendEvents, PatchingProgressData } from "../state/eventBus";
import { config, currentApplication, deviceInfo, refetchAppInfo, refetchModdingStatus, refetchSettings } from "../store";
import toast from "solid-toast";
import { createEffect } from "solid-js";
import { patchCurrentApp } from "../api/patching";
import RunButton from "../components/Buttons/RunButton";
import { CustomModal } from "./CustomModal";
import { Box, LinearProgress, Typography as MuiTypography } from "@suid/material";
import { For } from "solid-js";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { createStore } from "solid-js/store";
import { gotAccessToAppAndroidFolders, grantAccessToAppAndroidFolders, grantManageStorageAccess, hasManageStorageAccess, isPackageInstalled, launchCurrentApp, uninstallPackage } from "../api/android";
import { onMount } from "solid-js";
import { GetAndroidVersionName, IsOnQuest } from "../util";
import { restoreAppBackup } from "../api/backups";
import { useNavigate } from "@solidjs/router";
import { showConfirmModal } from "./ConfirmModal";
import { refetchMods } from "../state/mods";
import styled from "@suid/system/styled";
import { FiTrash } from 'solid-icons/fi'
import { FaSolidTrash } from "solid-icons/fa";
import { FirePatch } from "../assets/Icons";

enum IPatchingStage {
    Patching,
    Uninstalling,
    Installing,
    // TODO:
    Permissions,
    GameSpecific,
    Mods,
    Done
}

/**
 * Algo
 * 1) Patch the game
 * 2) Listen for patching events
 * 3) Update the progress bar
 * 4) When done, go to the next step of uninstalling the game
 * 5) Listen for uninstalling events
 * 6) When done, go to the next step of installing the game
 * 7) Listen for installing events
 * 8) When done, go to the next step of telling the user to allow the permissions
 * 9) Listen for permission events
 * 10) When done, go to the next step of asking the user to install core mods
 * 11) Listen for core mod events
 * 12) When done, go to the next step of asking the user to install mods or running the game
 * 13) Done
 */

async function checkIfGameIsInstalled() {
    let currentApplication = config()?.currentApp;
    if (!currentApplication) {
        toast.error("No game selected");
        return false;
    }
    let installationStatus = await isPackageInstalled(currentApplication);


    return installationStatus;

}

interface GlobalPatchingData {
    backupName?: string,
}

export default function PatchingModal(props: { open: boolean, onClose?: () => void, onPatchFinished?: () => void }) {

    const [patchingData, setPatchingData] = createStore<GlobalPatchingData>({});

    const [stage, setStage] = createSignal(IPatchingStage.Patching);

    return <>
        <Show when={props.open}>
            <Show when={stage() === IPatchingStage.Patching}>
                <PatchingStep setStage={setStage} open={props.open} onClose={props.onClose} setPatchingData={setPatchingData} />
            </Show>
            <Show when={stage() === IPatchingStage.Uninstalling}>
                <UninstallStep setStage={setStage} open={props.open} onClose={props.onClose} />
            </Show>
            <Show when={stage() === IPatchingStage.Installing}>
                <InstallStep setStage={setStage} open={props.open} onClose={props.onClose} patchingData={patchingData} />
            </Show>
            <Show when={stage() === IPatchingStage.Permissions}>
                <PermissionStep setStage={setStage} open={props.open} onClose={props.onClose} patchingData={patchingData} />
            </Show>
            {/* TODO: Add game specific */}
            <Show when={stage() === IPatchingStage.Done}>
                <DoneStep setStage={setStage} open={props.open} onClose={props.onClose} patchingData={patchingData} />
            </Show>
        </Show>
    </>
}



/**
 * Hete we patch the game and listen for patching events
 */
function PatchingStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void, setPatchingData: (data: Partial<GlobalPatchingData>) => void }) {
    const [progress, setProgress] = createSignal(0);

    // TODO: Check for free space before patching

    const [log, setLog] = createSignal<PatchingProgressData[]>([]);

    let logElement: HTMLPreElement | undefined;

    const [done, setDone] = createSignal(false);
    const [error, setError] = createSignal(false);
    const [inProgress, setInProgress] = createSignal(false);

    // Update log when a new event is received
    function onPatchProgress(e: CustomEvent) {
        let data = e.detail as PatchingProgressData;
        console.log(data);
        console.log(currentApplication);

        // find previous operation
        let prevOperation = log().find((l) => l.currentOperation === data.currentOperation);

        if (prevOperation) {
            // if the operation is the same, replace it
            setLog((old) => old.map((l) => {
                if (l.currentOperation === data.currentOperation) {
                    return data;
                }
                return l;
            }))
        } else {
            // if the operation is not the same, add it
            setLog((old) => [...old, data]);
        }

        logElement?.scrollTo(0, logElement.scrollHeight);

        setProgress(data.progress * 100);

        if (data.done) {
            if (data.error) {
                setError(true);
                toast.error("Failed to patch the game");
            } else {
                setDone(true);
                props.setPatchingData({ backupName: data.backupName });
                toast.success("Game patched successfully");

            }

            // Remove the listener
            // @ts-ignore
            BackendEvents.removeEventListener("patch-progress", onPatchProgress);

        }


    }

    async function startPatching() {
        setInProgress(true);
        try {
            let result = await patchCurrentApp();
            if (!result) {
                toast.error("Failed to patch the game");
                return;
            }
            await refetchModdingStatus();
            setInProgress(true);
        } catch (e) {
            console.error(e)
            toast.error("Failed to patch game");
            setInProgress(false);
            setError(true);
        }

        // @ts-ignore
        BackendEvents.addEventListener("patch-progress", onPatchProgress);
    }

    onCleanup(() => {
        // @ts-ignore
        BackendEvents.removeEventListener("patch-progress", onPatchProgress);
    })
    return (
        <CustomModal title={"Patching"} open={props.open} onClose={props.onClose}
            buttons={<>
                {/* If success, allow to go to next step (Uninstalling) */}
                <Show when={done() && !error()}>
                    <RunButton text="Next step" icon={<PlayArrowRounded />} variant='success' onClick={() => { props.setStage(IPatchingStage.Uninstalling) }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && !inProgress()}>
                    <RunButton text="Patch" icon={<FirePatch />} variant='success' onClick={startPatching} />
                </Show>



            </>} >
            <Show when={!inProgress() && !done()}>
                <MediumText>
                    To start the patching process, click on the button below.
                </MediumText>
            </Show>

            <Show when={inProgress()}>
                <Box sx={{ width: "100%" }}>
                    <LinearProgress variant="determinate" value={progress()} />
                </Box>
                <pre ref={logElement} style={{
                    background: "black",
                    color: "white",
                    padding: "10px",
                    "border-radius": "0px",
                    "min-width": "400px",
                    "max-width": "100vw",
                    height: "300px",
                    "overflow-y": "auto",
                    "font-size": "12px",
                }}>
                    <For each={log()}>
                        {(line) => <LogLine line={line} />}
                    </For>
                </pre>
            </Show>
            {/* If done succesfully */}
            <Show when={!inProgress() && done() && !error()}>
                <MediumText>
                    The patching is completed successfully.
                    To continue, click on the button below.
                </MediumText>

            </Show>

        </CustomModal>
    )
}

function LogLine({ line }: { line: PatchingProgressData }) {
    return <div>({line.doneOperations - 1}/{line.totalOperations}) {line.currentOperation}{line.done ? "OK" : "..."} </div>
}


async function uninstallGame() {
    let currentGame = config()?.currentApp;

    if (!currentGame) return toast.error("No game selected! Open Change App modal and select a game.")

    if (!IsOnQuest()) {
        toast("Uninstall dialog is open on quest itself!")
    }

    if (!await isPackageInstalled(currentGame)) {
        return toast.error("Game is not installed, install the game to delete it lol!")
    }

    await uninstallPackage(currentGame);
}

/**
 * Here we ask to uninstall the game and check if it's uninstalled
 * @param props 
 * @returns 
 */
function UninstallStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void }) {

    const [done, setDone] = createSignal(false);
    const [error, setError] = createSignal(false);
    const [inProgress, setInProgress] = createSignal(false);
    const [isInstalled, setIsInstalled] = createSignal(true);

    const timer: NodeJS.Timer = setInterval(async () => {
        if (!inProgress()) return;
        if (!isInstalled()) return;

        let installed = await checkIfGameIsInstalled();

        // If the gaME IS UNINSTALLED
        if (!installed) {
            setIsInstalled(false);
            setDone(true);
            setInProgress(false);
            clearInterval(timer);
            toast.success("Game uninstalled successfully");
        }
    }, 400);


    // Skip this step if the game is not installed already
    onMount(async () => {
        setInProgress(true);

        let currentApp = config()?.currentApp;

        if (!currentApp) {
            toast.error("No game selected");
            return;
        };

        if (!(await checkIfGameIsInstalled())) {
            props.setStage(IPatchingStage.Installing);
            return;
        }
    });

    onCleanup(() => {
        if (timer) clearInterval(timer);
    });

    return (
        <CustomModal title={"Uninstall the game"} open={props.open} onClose={props.onClose}
            buttons={<>
                {/* If success, allow to go to next step (Uninstalling) */}
                <Show when={done() && !error() && !isInstalled()}>
                    <RunButton text="Next step" variant='success' onClick={() => { props.setStage(IPatchingStage.Installing) }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && isInstalled()}>
                    <RunButton text="Uninstall" icon={<FaSolidTrash />} variant='error' onClick={uninstallGame} />
                </Show>
            </>} >

            <Show when={isInstalled()}>
                <MediumText>
                    To install patched version of the game, you need to uninstall the original game first.
                </MediumText>
                <MediumText>
                    Press uninstall and confirm the uninstallation on the quest. After that, click on the button below to go to the next step.
                </MediumText>
                {/* Tell the user more info? */}
            </Show>

            <Show when={!isInstalled()}>
                <MediumText>
                    The game is successfully uninstalled, click on the button below to go to the next step.
                </MediumText>

            </Show>

        </CustomModal>
    )
}

async function installGame(name: string) {
    let currentGame = config()?.currentApp;

    if (!currentGame) return toast.error("No game selected! Open Change App modal and select a game.")

    if (!IsOnQuest()) {
        toast("Install dialog is open on quest itself!")
    }

    if (await isPackageInstalled(currentGame)) {
        return toast.error("Game is already installed, uninstall the game to install it again lol!")
    }

    await restoreAppBackup(currentGame, name);
}

/**
 * Here we ask to install the game
 * @param props 
 * @returns 
 */
function InstallStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void, patchingData: GlobalPatchingData },) {

    const [done, setDone] = createSignal(false);
    const [error, setError] = createSignal(false);
    const [inProgress, setInProgress] = createSignal(false);
    const [isInstalled, setIsInstalled] = createSignal(false);

    const timer: NodeJS.Timer = setInterval(async () => {
        if (!inProgress()) return;
        if (isInstalled()) return;

        let installed = await checkIfGameIsInstalled();

        if (installed) {
            setIsInstalled(true);
            setDone(true);
            setInProgress(false);
            clearInterval(timer);
            toast.success("Game is installed successfully");
        }
    }, 400);

    onMount(async () => {
        setInProgress(true);

        let currentApp = config()?.currentApp;

        if (!currentApp) {
            toast.error("No game selected");
            return;
        };

        if (await checkIfGameIsInstalled()) {
            setIsInstalled(true);
            await installGame(props.patchingData!.backupName!);
        } else {
            // Skip if installed already
            props.setStage(IPatchingStage.Installing);
            return;
        }
    });


    onCleanup(() => {
        if (timer) clearInterval(timer);
    });

    return (
        <CustomModal title={"Install the patched game"} open={props.open} onClose={props.onClose}
            buttons={<>
                {/* If success, allow to go to next step (Uninstalling) */}
                <Show when={done() && !error() && isInstalled()}>
                    <RunButton text="Next step" variant='success' onClick={() => { props.setStage(IPatchingStage.Installing) }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && !isInstalled()}>
                    <RunButton text="Install" variant='success' onClick={() => installGame(props.patchingData.backupName!)} />
                </Show>
            </>} >
            <Show when={isInstalled()}>
                <MediumText>
                    The game is successfully <span class="text-accent">installed</span>, click on the button below to go to the next step.
                </MediumText>
            </Show>

            <Show when={!isInstalled()}>
                <MediumText>
                    Install the game on your quest, click on the button below to install the game.
                    After that, click on the button below to go to the next step.
                </MediumText>
                <SmallText>
                    If the game won't install, check your free space on the quest. You should have at least 3gb free.
                    If you have enough space, try to restart the quest and try again.
                    One more issue could be that you have the game badly uninstalled, in this case you need to run adb command to uninstall the game using SideQuest.
                </SmallText>
            </Show>
        </CustomModal>
    )
}




/**
 * Here we ask to install the game
 * @param props 
 * @returns 
 */
function PermissionStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void, patchingData: GlobalPatchingData },) {

    /**
     * The current device sdk version
     * 34 = Android 14 (Beta)
     * 33 = Android 13
     * 32 = Android 12L
     * 31 = Android 12
     * 30 = Android 11
     * 29 = Android 10
     * 28 = Android 9
     * 0 = Unknown
     */
    const [sdkVersion, setsdkVersion] = createSignal<number>(deviceInfo()?.sdkVersion ?? 0);

    // Access to app dirs is granted
    const [appDirGranted, setAppDirGranted] = createSignal(false);
    const [appManageAccessGranted, setAppManageAccessGranted] = createSignal(false);

    const [done, setDone] = createSignal(false);

    async function grantAppDirAccess() {
        let currentApp = config()?.currentApp;
        if (!currentApp) return toast.error("No game selected");

        await grantAccessToAppAndroidFolders(currentApp);
    }

    async function grantManageStorageToApp() {
        let currentApp = config()?.currentApp;
        if (!currentApp) return toast.error("No game selected");
        await grantManageStorageAccess(currentApp);
    }


    const timer: NodeJS.Timer = setInterval(async () => {
        let currentApp = config()?.currentApp;
        if (!currentApp) return toast.error("No game selected");

        // If we are on Android 11 or and below 13, we need to grant access to the app dir
        if (sdkVersion() > 29 && sdkVersion() < 33) {
            let gotAccess = await gotAccessToAppAndroidFolders(currentApp);
            if (appDirGranted() != gotAccess) {
                setAppDirGranted(gotAccess);
            }
        };

        if (sdkVersion() > 29) {
            let gotAccess = await hasManageStorageAccess(currentApp);

            if (appManageAccessGranted() != gotAccess) {
                setAppManageAccessGranted(gotAccess);
            }
        }

    }, 400);


    let allowToProceed = createMemo(() => {
        if (sdkVersion() >= 30) return appDirGranted() && appManageAccessGranted();

        if (sdkVersion() < 30) return true;
    })

    onCleanup(() => {
        if (timer) clearInterval(timer);
    });

    return (
        <CustomModal title={"Allow required permissions"} open={props.open} onClose={props.onClose}
            buttons={<>
                {/* If success, allow to go to next step (Done) */}
                <RunButton disabled={!allowToProceed()} text="Next step" icon={<PlayArrowRounded />} variant='success' onClick={() => { props.setStage(IPatchingStage.Done) }} />
            </>} >

            <Show when={sdkVersion() >= 33}>
                <MediumText>
                    Your android version is unsupported, proceed at your own risk <span >{GetAndroidVersionName(sdkVersion())}</span>
                </MediumText>
            </Show>

            <Show when={allowToProceed()}>
                <MediumText class="text-accent" >
                    Every permission is granted, you can proceed to the next step
                </MediumText>
            </Show>
            <Show when={!allowToProceed()}>
                <MediumText>
                    To allow the game to run properly, we need to grant some permissions to the game.
                </MediumText>
                <MediumText>
                    Please allow the permissions on your Quest after pressing the buttons below.
                    All the buttons need to fade out to continue.
                </MediumText>
            </Show>


            {/* Permission buttons */}
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', mt: 2, gap: "5px" }}>
                <Show when={sdkVersion() >= 30}>
                    {/* Allows the game to access mod data (not in the modloader rn, just to be safe we will add it here) */}
                    <RunButton disabled={appManageAccessGranted()} text="Allow manage storage permission for the app" onClick={grantManageStorageToApp} />
                </Show>
                <Show when={sdkVersion() > 29 && sdkVersion() < 33}>
                    {/* Gets access to APP dir by using Android/data/ access, Android 13 requires the game to be run once */}
                    <RunButton disabled={appDirGranted()} text="Allow QAVS to access Android/data/ & Android/obb/" onClick={grantAppDirAccess} />
                </Show>
            </Box>
        </CustomModal>
    )
}




/**
 * Here we ask to install the game
 * @param props 
 * @returns 
 */
function DoneStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void, patchingData: GlobalPatchingData },) {
    let navigate = useNavigate();
    function startGame() {
        launchCurrentApp();
    }

    // Refresh everything just to be sure
    onMount(() => {
        refetchModdingStatus();
        refetchAppInfo();
        refetchMods();
        refetchSettings();
    })

    return (
        <CustomModal title={"Patching is done!"} open={props.open} onClose={props.onClose}
            buttons={<>

                {/* If success, close the modal */}
                <RunButton text="Start game" icon={<PlayArrowRounded />} onClick={async () => {
                    let sure = await showConfirmModal({
                        title: "Start game",
                        message: "Are you sure you want to start the game without installing mods? Patched game with no mods does not make sense if you are not a developer.",
                    })

                    if (sure) {
                        props?.onClose && props.onClose(); startGame();
                    } else {
                        return;
                    }
                }} />

                <RunButton text="Install mods" variant='success' onClick={() => {
                    props?.onClose && props.onClose();
                    navigate("/mods");
                }
                } />
            </>} >

            <SmallText>
                The game is successfully installed, you can now start the game or install the mods.
            </SmallText>
        </CustomModal>
    )
}


// Styles
const SmallText = styled(MuiTypography)({
    fontFamily: 'Roboto',
    fontStyle: 'normal',
    fontWeight: 400,
    fontSize: '12px',
    lineHeight: '16px',
    color: '#FFFFFF',
    marginBottom: '5px',
});

const MediumText = styled(MuiTypography)({
    fontFamily: 'Roboto',
    fontStyle: 'normal',
    fontWeight: 400,
    fontSize: '14px',
    lineHeight: '16px',
    color: '#FFFFFF',
    marginBottom: '5px',
});