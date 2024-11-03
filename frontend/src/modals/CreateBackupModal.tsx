import { Accessor, For, Show, createEffect, createMemo, createSignal, onCleanup, onMount, splitProps } from "solid-js";
import { CustomModal } from "./CustomModal";
import { Box, FormControlLabel, Input, LinearProgress, Switch, TextField, Typography } from "@suid/material";
import { OptionHeader } from "../pages/ToolsPage";
import RunButton from "../components/Buttons/RunButton";
import { backupList, refetchBackups } from "../state/backups";
import toast from "solid-toast";
import { createBackup } from "../api/backups";
import { config, moddingStatus } from "../store";
import { RemoveVersionUnderscore } from "../util";
import { BackupProgressData } from "../state/eventBus";
import { BackendEvents } from "../state/eventBus";
import { Setter } from "solid-js";
import { MediumText, TitleText } from "../styles/TextStyles";

enum Stage {
    Form,
    Progress
}

export default function CreateBackupModal(props: { open: boolean, onClose: () => void }) {
    let [local, other] = splitProps(props, ['open', 'onClose']);

    let [stage, setStage] = createSignal(Stage.Form);
    const [backupName, setBackupName] = createSignal("");

    function onClose() {
        // Reset the stage
        setStage(Stage.Form);
        setBackupName("");
        props.onClose?.();
    }

    return (<CustomModal title="Create backup" open={local.open} onClose={local.onClose} onBackdropClick={local.onClose}>
        <Show when={stage() == Stage.Form}>
            <BackupCreationForm onClose={onClose} nextStage={() => { setStage(Stage.Progress) }} backupName={backupName()} setBackupName={setBackupName} />
        </Show>
        <Show when={stage() == Stage.Progress}>
            <BackupProgressView onClose={onClose} backupName={backupName()} />
        </Show>
    </CustomModal>)
}

function BackupCreationForm(props: {
    onClose?: () => void,
    nextStage?: () => void,
    backupName: string,
    setBackupName?: Setter<string>,
}
) {
    const [nameChanged, setNameChanged] = createSignal(false);
    
    const [onlyData, setOnlyData] = createSignal(false);

    // Set default backup name
    createEffect(() => {
        if (nameChanged()) return;

        let currentApp = config()?.currentApp;
        let version = moddingStatus()?.version;
        let appName = config()?.currentAppName;
        let isModded = moddingStatus()?.isPatched;

        if (currentApp == null || version == null) return;

        let nameParts = [];

        // nameParts.push(appName || currentApp );
        nameParts.push(RemoveVersionUnderscore(version));

        if (onlyData()) (nameParts.push("data"));
        if (isModded) (nameParts.push("modded"));

        props?.setBackupName?.(nameParts.join("_"));
    })

    // Some dum validation
    const invalidName = createMemo(() => {
        let name = props.backupName;
        // Check if the name clashes with an existing backup
        if (backupList() != null && backupList()?.backups != null) {
            for (let backup of backupList()!.backups) {
                if (backup.backupName == name) {
                    return true;
                }
            }
        }
        if (name == "") return true;
        return false;
    })

    return (<form style={{
        "min-width": "300px",
    }}
        onSubmit={async (e) => {
            e.preventDefault();

            if (invalidName()) {
                return toast.error("Backup name already exists");
            }

            let app = config()?.currentApp;
            if (app == null) {
                return toast.error("No app selected");
            }

            await createBackup(app, props.backupName, onlyData());
            props.nextStage?.();
        }}
    >
        <Box class="my-3 bg-slate-950 p-4 rounded-md">
            <TitleText>Current app</TitleText>
            <MediumText>App: {config()?.currentApp}</MediumText>
            <MediumText>Version: {moddingStatus()?.version}</MediumText>
            <MediumText>Modded: {moddingStatus()?.isPatched ? <span class="text-green-300">Yes</span> : <span class="text-red-600">No</span>}</MediumText>    
            <MediumText>App name: {config()?.currentAppName}</MediumText>
        </Box>
        <TextField name="name"
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
            }} />

        <FormControlLabel class="pt-3 pb-3"
            control={<Switch checked={onlyData()} onChange={(e, value) => {
                console.log(value);
                setOnlyData(value)
            }} />}
            label="Only backup app data"
        />
        

        <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end"  }}>
            <RunButton text="Create backup" variant="success" type="submit" />
        </Box>
    </form>)
}

function BackupProgressView(props: {
    onClose?: () => void,
    nextStage?: () => void,
    backupName: string,
}
) {
    const [progress, setProgress] = createSignal(0);

    // TODO: Check for free space before patching

    const [log, setLog] = createSignal<BackupProgressData[]>([]);

    let logElement: HTMLPreElement | undefined;

    const [done, setDone] = createSignal(false);
    const [error, setError] = createSignal(false);

    // Update log when a new event is received
    function onBackupProgress(e: CustomEvent) {
        let data = e.detail as BackupProgressData;

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
                // props.setPatchingData({ backupName: data.backupName });
                toast.success("Game patched successfully");
            }

            // Remove the listener
            // @ts-ignore
            BackendEvents.removeEventListener('backup-progress', onBackupProgress);
        }

    }

    onMount(() => {
        // @ts-ignore
        BackendEvents.addEventListener('backup-progress', onBackupProgress);
    })

    onCleanup(() => {
        // @ts-ignore
        BackendEvents.removeEventListener('backup-progress', onBackupProgress);
    })


    return (<>
        
        <Show when={done()}>
            <Show when={!error()}>
                <MediumText>
                    Backup created successfully<br></br>
                    Backup Name is {props.backupName}
                </MediumText>
            </Show>

            <Show when={error()}>
                <MediumText>
                    Failed to create backup
                </MediumText>
            </Show>

            <Box sx={{ flexGrow: 1, display: "flex", justifyContent: "end"  }}>
                <RunButton text="Close" variant={(error()?"error":"success")} onClick={() => {
                    props.onClose?.();
                }} />
            </Box>

        </Show>
        <Show when={!done() || error()}>
            <pre ref={logElement} class="bg-black text-white p-2 rounded-none max-w-xs h-28 overflow-y-auto text-sm ">
                <For each={log()}>
                    {(line) => <LogLine line={line} />}
                </For>
            </pre>

            <Box class="w-full">
                <LinearProgress variant="determinate" value={progress()} />
            </Box>
        </Show>
    </>
    );
}

function LogLine({ line }: { line: BackupProgressData }) {
    return <div>({line.doneOperations - 1}/{line.totalOperations}) {line.currentOperation}{line.done ? "OK" : "..."} </div>
}