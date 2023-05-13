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

    return <CustomModal open={props.open} onClose={props.onClose}>

        <Show when={stage() === IRestoreStage.Form}>
            <RestoreForm next={() => { setStage(IRestoreStage.Uninstalling) }} onClose={props.onClose} setOptions={setOptions} options={options} />
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
        <Box sx={{
            paddingTop: "10px",
            paddingBottom: "10px",
        }}>
            <TitleText>Currently installed app information</TitleText>
            <MediumText>App info</MediumText>
            <MediumText>App: {config()?.currentApp}</MediumText>
            <MediumText>Version: {moddingStatus()?.version}</MediumText>
            <MediumText>Modded: {moddingStatus()?.isPatched ? "Yes" : "No"}</MediumText>
            <MediumText>App name: {config()?.currentAppName}</MediumText>
        </Box>
        {/* <TextField name="name"
            helperText={invalidName() && "Name already exists"}
            error={invalidName()}
            fullWidth size="small"
            id="filled-basic"
            label="Backup name"
            variant="filled"
            value={props.backupName}
            onChange={(value) => {
                // If the user has changed the name, don't change it anymore
                !nameChanged() && setNameChanged(true);
                // Set the name
                props.setBackupName?.(value.target.value)
            }} /> */}

        <FormControlLabel sx={{
            pt: 1
        }}

            control={<Switch checked={props.options.restoreData} onChange={(e, value) => {
                console.log(value);
                props.setOptions("restoreData", value)
            }} />}
            label="Only backup app data"
        />


        <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end" }}>
            <RunButton text="Create backup" variant="success" type="submit" />
        </Box>
    </form>
}