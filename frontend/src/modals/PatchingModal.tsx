import { Show, createSignal } from "solid-js";
import { createStore } from "solid-js/store";
import { PermissionStep } from "./InstallSteps/PermissionsStep";
import { PatchingStep } from "./InstallSteps/PatchingStep";
import { InstallStep } from "./InstallSteps/InstallStep";
import { UninstallStep } from "./InstallSteps/UninstallStep";
import { DoneStep } from "./InstallSteps/DoneStep";

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


export interface GlobalPatchingData {
    backupName?: string,
}

export default function PatchingModal(props: { open: boolean, onClose?: () => void, onPatchFinished?: () => void }) {
    const [patchingData, setPatchingData] = createStore<GlobalPatchingData>({});

    const [stage, setStage] = createSignal(IPatchingStage.Patching);

    return <>
        <Show when={props.open}>
            <Show when={stage() === IPatchingStage.Patching}>
                <PatchingStep
                    next={() => { setStage(IPatchingStage.Uninstalling); }} onClose={props.onClose}
                    setBackupName={(name) => {
                        setPatchingData({ backupName: name });
                    }}
                />
            </Show>
            <Show when={stage() === IPatchingStage.Uninstalling}>
                <UninstallStep next={() => { setStage(IPatchingStage.Installing); }} onClose={props.onClose} />
            </Show>
            <Show when={stage() === IPatchingStage.Installing}>
                <InstallStep next={() => { setStage(IPatchingStage.Permissions); }} onClose={props.onClose} backupName={patchingData.backupName!} />
            </Show>
            <Show when={stage() === IPatchingStage.Permissions}>
                <PermissionStep next={() => { setStage(IPatchingStage.Done); }} />
            </Show>
            {/* TODO: Add game specific */}
            <Show when={stage() === IPatchingStage.Done}>
                <DoneStep next={() => {
                    props.onPatchFinished?.();
                }} onClose={props.onClose} patchingData={patchingData} />
            </Show>
        </Show>
    </>
}
