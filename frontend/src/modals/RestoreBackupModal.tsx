import { Show, createMemo, createSignal, onCleanup } from "solid-js";
import { BackendEvents, PatchingProgressData } from "../state/eventBus";
import { config, currentApplication, deviceInfo, moddingStatus, refetchAppInfo, refetchModdingStatus, refetchSettings } from "../store";
import toast from "solid-toast";
import { createEffect } from "solid-js";
import { patchCurrentApp } from "../api/patching";
import RunButton from "../components/Buttons/RunButton";
import { CustomModal } from "./CustomModal";
import { Box, FormControlLabel, LinearProgress, Typography as MuiTypography, Switch, TextField } from "@suid/material";
import { For } from "solid-js";
import PlayArrowRounded from "@suid/icons-material/PlayArrowRounded";
import { SetStoreFunction, createStore } from "solid-js/store";
import { gotAccessToAppAndroidFolders, grantAccessToAppAndroidFolders, grantManageStorageAccess, hasManageStorageAccess, isPackageInstalled, launchCurrentApp, uninstallPackage } from "../api/android";
import { onMount } from "solid-js";
import { GetAndroidVersionName, IsOnQuest } from "../util";
import { IBackup, restoreAppBackup } from "../api/backups";
import { useNavigate } from "@solidjs/router";
import { showConfirmModal } from "./ConfirmModal";
import { refetchMods } from "../state/mods";
import styled from "@suid/system/styled";
import { FiTrash } from 'solid-icons/fi'
import { FaSolidTrash } from "solid-icons/fa";
import { FirePatch } from "../assets/Icons";
import { MediumText, SmallText, TitleText } from "../styles/TextStyles";

enum IRestoreStage {
    Form,
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

interface GlobalRestoringData {
    backupName?: string,
}

export default function RestoreBackupModal(props: {
    open: boolean, onClose?: () => void,
    onPatchFinished?: () => void,
    selectedBackup?: IBackup,
}) {
    const [options, setOptions] = createStore<{
        restoreData: boolean,
        backupData?: IBackup
    }>({
        restoreData: false,
    });
    const [stage, setStage] = createSignal(IRestoreStage.Form);

    /**
     * Used to update the stage and skip stages if needed
     * @param nextStage 
     */
    function UpdateStage(nextStage: IRestoreStage) {
        let currentStage = stage;

        setStage(nextStage);
    }

    function internalOnClose() {
        props.onClose?.();

        // Reset the stage
        setStage(IRestoreStage.Form);
    }


    return <CustomModal open={props.open} onClose={internalOnClose} title="Restore">
        <Show when={stage() === IRestoreStage.Form}>
            <RestoreForm
                next={() => { UpdateStage(IRestoreStage.Uninstalling) }}
                onClose={internalOnClose}
                setOptions={setOptions}
                options={options}
                selectedBackup={props.selectedBackup}
            />
        </Show>
        <Show when={stage() === IRestoreStage.Uninstalling}>
            <UninstallingStep
                next={() => { UpdateStage(IRestoreStage.Installing) }}
                onClose={internalOnClose}
                setOptions={setOptions}
                options={options}
                selectedBackup={props.selectedBackup}
            />
        </Show>
        <Show when={stage() === IRestoreStage.Installing}>
            <InstallingStep
                next={() => { UpdateStage(IRestoreStage.Permissions) }}
                onClose={internalOnClose}
                setOptions={setOptions}
                options={options}
                selectedBackup={props.selectedBackup}
            />
        </Show>
        <Show when={stage() === IRestoreStage.Permissions}>
            <PermissionStep
                next={() => { UpdateStage(IRestoreStage.Done) }}
                onClose={internalOnClose}
                setOptions={setOptions}
                options={options}
                selectedBackup={props.selectedBackup}
            />
        </Show>
    </CustomModal>
}



function RestoreForm(props: {
    options: {
        restoreData: boolean;
        backupData?: IBackup | undefined;
    },
    onClose?: () => void,
    setOptions: SetStoreFunction<{
        restoreData?: boolean;
        backupData?: IBackup | undefined;
    }>,
    next?: () => void;
    selectedBackup?: IBackup,
}) {
    return <form style={{
        "min-width": "300px",
    }}
        onSubmit={async (e) => {
            e.preventDefault();

            // if (invalidName()) {
            //     return toast.error("Backup name already exists");
            // }

            let app = config()?.currentApp;
            if (app == null) {
                return toast.error("No app selected");
            }

            // await restore(app, props.backupName, onlyData());
            props.next?.();
        }}
    >

        <Box class="my-3 bg-slate-950 p-4 rounded-md">
            <TitleText>Selected backup information</TitleText>
            <MediumText>Backup name: {props.selectedBackup?.backupName}</MediumText>
            <MediumText>Size: {props.selectedBackup?.backupSizeString}</MediumText>
            <MediumText>Modded: {props.selectedBackup?.isPatchedApk ? <span class="text-green-300">Yes</span> : <span class="text-red-600">No</span>}</MediumText>
            <MediumText>Contains apk: {props.selectedBackup?.containsApk ? <span class="text-green-300">Yes</span> : <span class="text-red-600">No</span>}</MediumText>
            <MediumText>Contains gamedata: {props.selectedBackup?.containsGamedata ? <span class="text-green-300">Yes</span> : <span class="text-red-600">No</span>}</MediumText>
            <MediumText class="max-w-xs wrap" sx={{
                wordBreak: "break-all"
            }}>Location: {props.selectedBackup?.backupLocation}</MediumText>
        </Box>

        <FormControlLabel sx={{
            pt: 1
        }}

            control={<Switch checked={props.options.restoreData} onChange={(e, value) => {
                console.log(value);
                props.setOptions("restoreData", value)
            }} />}
            label="Restore app data"
        />


        <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
            <RunButton text="Cancel" onClick={props.onClose} />
            <RunButton text="Start restore" variant="success" type="submit" />
        </Box>
    </form>
}

function UninstallingStep(props: {
    options: {
        restoreData: boolean;
        backupData?: IBackup | undefined;
    },
    onClose?: () => void,
    setOptions: SetStoreFunction<{
        restoreData?: boolean;
        backupData?: IBackup | undefined;
    }>,
    next?: () => void;
    selectedBackup?: IBackup,
}) {

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
            props.next?.();
            return;
        }
    });

    onCleanup(() => {
        if (timer) clearInterval(timer);
    });



    return <div>
        <Show when={isInstalled()}>
            <MediumText>
                To install backup of the game, you need to uninstall the original game first.
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

        <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
            {/* If success, allow to go to next step (Uninstalling) */}
            <Show when={done() && !error() && !isInstalled()}>
                <RunButton text="Next step" variant='success' onClick={() => { props.next?.() }} />
            </Show>
            {/* If just started show the patch button */}
            <Show when={!done() && !error() && isInstalled()}>
                <RunButton text="Uninstall" icon={<FaSolidTrash />} variant='error' onClick={uninstallGame} />
            </Show>
        </Box>
    </div>
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

function InstallingStep(props: {
    options: {
        restoreData: boolean;
        backupData?: IBackup | undefined;
    },
    onClose?: () => void,
    setOptions: SetStoreFunction<{
        restoreData?: boolean;
        backupData?: IBackup | undefined;
    }>,
    next?: () => void;
    selectedBackup?: IBackup,
}) {

    const [done, setDone] = createSignal(false);
    const [error, setError] = createSignal(false);
    const [inProgress, setInProgress] = createSignal(false);
    const [isInstalled, setIsInstalled] = createSignal(false);

    const timer: NodeJS.Timer = setInterval(async () => {
        if (!inProgress()) return;
        if (isInstalled()) return;

        let installed = await checkIfGameIsInstalled();

        if (installed) {
            debugger
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
        
        let isInstalled = await checkIfGameIsInstalled();
        if (isInstalled) {
            setIsInstalled(true);
        }
        // Skipping a step will be handled above
    });


    onCleanup(() => {
        if (timer) clearInterval(timer);
    });

    return (
        <div>
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
            <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
                {/* If success, allow to go to next step (Uninstalling) */}
                <Show when={done() && !error() && isInstalled()}>
                    <RunButton text="Next step" variant='success' onClick={() => { props.next?.() }} />
                </Show>
                {/* If just started show the patch button */}
                <Show when={!done() && !error() && !isInstalled()}>
                    <RunButton text="Install" variant='success' onClick={() => installGame(props.selectedBackup!.backupName)} />
                </Show>
            </Box>
        </div>
    )
}


/**
 * Here we ask to install the game
 * @param props 
 * @returns 
 */
function PermissionStep(props: {
    options: {
        restoreData: boolean;
        backupData?: IBackup | undefined;
    },
    onClose?: () => void,
    setOptions: SetStoreFunction<{
        restoreData?: boolean;
        backupData?: IBackup | undefined;
    }>,
    next?: () => void;
    selectedBackup?: IBackup,

},) {

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
        // If the apk is patched then we can require appManageAccess
        if (props.selectedBackup?.isPatchedApk) {
            if (sdkVersion() >= 30) return appDirGranted() && appManageAccessGranted();
            if (sdkVersion() < 30) return true;
        } else {
            if (sdkVersion() >= 30) return appDirGranted();
            if (sdkVersion() < 30) return true;
        }
    })

    onCleanup(() => {
        if (timer) clearInterval(timer);
    });

    return (
        <div>

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
                <Show when={props.selectedBackup?.isPatchedApk && sdkVersion() >= 30}>
                    {/* Allows the game to access mod data (not in the modloader rn, just to be safe we will add it here) */}
                    <RunButton disabled={appManageAccessGranted()} text="Allow manage storage permission for the app" onClick={grantManageStorageToApp} />
                </Show>
                <Show when={sdkVersion() > 29 && sdkVersion() < 33}>
                    {/* Gets access to APP dir by using Android/data/ access, Android 13 requires the game to be run once */}
                    <RunButton disabled={appDirGranted()} text="Allow QAVS to access Android/data/ & Android/obb/" onClick={grantAppDirAccess} />
                </Show>
            </Box>
            <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end", gap: 2 }}>
                {/* If success, allow to go to next step (Done) */}
                <RunButton disabled={!allowToProceed()} text="Next step" icon={<PlayArrowRounded />} variant='success' onClick={() => { props.next?.() }} />
            </Box>
        </div>
    )
}