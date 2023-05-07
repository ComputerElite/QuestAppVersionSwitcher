import { Show, createSignal, onCleanup } from "solid-js";
import { BackendEvents, PatchingProgressData } from "../state/eventBus";
import { config, currentApplication, refetchModdingStatus } from "../store";
import toast from "solid-toast";
import { createEffect } from "solid-js";
import { patchCurrentApp } from "../api/patching";
import RunButton from "../components/Buttons/RunButton";
import { CustomModal } from "./CustomModal";
import { Box, LinearProgress, Typography } from "@suid/material";
import { For } from "solid-js";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { createStore } from "solid-js/store";
import { isPackageInstalled, uninstallPackage } from "../api/android";
import { onMount } from "solid-js";
import { IsOnQuest } from "../util";
import { restoreAppBackup } from "../api/backups";

enum IPatchingStage {
    Patching,
    Uninstalling,
    Installing,
    // TODO:
    Permissions,
    CoreMods,
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
                <InstallStep setStage={setStage} open={props.open} onClose={props.onClose} patchingData={patchingData}  />
            </Show>
        </Show>
    </>
}




function PatchingStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void, setPatchingData: (data: Partial<GlobalPatchingData>) => void }) {
    const [progress, setProgress] = createSignal(0);

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
        }

        // @ts-ignore
        BackendEvents.addEventListener("patch-progress", onPatchProgress);
    }

    onCleanup(() => {
        // @ts-ignore
        BackendEvents.removeEventListener("patch-progress", onPatchProgress);
    })
    return (
        <CustomModal title={"Patching modal"} open={props.open} onClose={props.onClose}
            buttons={<>
                {/* If success, allow to go to next step (Uninstalling) */}
                <Show when={done() && !error()}>
                    <RunButton text="Next step" icon={<PlayArrowRounded />} variant='success' onClick={() => { props.setStage(IPatchingStage.Uninstalling) }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && !inProgress()}>
                    <RunButton text="Patch" icon={<PlayArrowRounded />} variant='success' onClick={startPatching} />
                </Show>



            </>} >
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
 * Here we ask to uninstall the game
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

    onMount(async () => {
        setInProgress(true);

        let currentApp = config()?.currentApp;

        if (!currentApp) {
            toast.error("No game selected");
            return;
        };

        if (await checkIfGameIsInstalled()) {
            setIsInstalled(true);
            await uninstallGame();
        } else {
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
                    <RunButton text="Next step" icon={<PlayArrowRounded />} variant='success' onClick={() => { props.setStage(IPatchingStage.Installing) }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && isInstalled()}>
                    <RunButton text="Uninstall" icon={<PlayArrowRounded />} variant='error' onClick={uninstallGame} />
                </Show>
            </>} >
            <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                Uninstalling the game, please confirm the uninstallation on your Quest
            </Typography>

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
function InstallStep(props: { open: boolean, setStage: (stage: IPatchingStage) => void, onClose?: () => void, patchingData: GlobalPatchingData }, ) {

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
                    <RunButton text="Next step" icon={<PlayArrowRounded />} variant='success' onClick={() => { props.setStage(IPatchingStage.Installing) }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && !isInstalled()}>
                    <RunButton text="Install" icon={<PlayArrowRounded />} variant='error' onClick={uninstallGame} />
                </Show>
            </>} >
            <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                Install the game, please confirm the installation on your Quest after pressing the button below
            </Typography>

        </CustomModal>
    )
}