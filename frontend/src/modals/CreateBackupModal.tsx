import { createMemo, createSignal, splitProps } from "solid-js";
import { CustomModal } from "./CustomModal";
import { Box, FormControlLabel, Input, Switch, TextField } from "@suid/material";
import { OptionHeader } from "../pages/ToolsPage";
import RunButton from "../components/Buttons/RunButton";
import { backupList } from "../state/backups";
import toast from "solid-toast";

export default function CreateBackupModal(props: { open: boolean, onClose: () => void }) {
    let [local, other] = splitProps(props, ['open', 'onClose']);

    const [backupName, setBackupName] = createSignal("");
    const [onlyData, setOnlyData] = createSignal(false);
    
    // Some dum validation
    const invalidName = createMemo(() => {
        let name = backupName();
        // Check if the name clashes with an existing backup
        if (backupList() != null && backupList()?.backups != null) {
            for (let backup of backupList()!.backups) {
                if (backup.backupName == name) {
                    return true;
                }
            }
        }
        return false;
    })

    return (<CustomModal title="Create backup" open={local.open} onClose={local.onClose} onBackdropClick={local.onClose}>
        
        <form style={{
            "min-width": "300px",
        }}
            onSubmit={(e) => {
                e.preventDefault();
                console.log(backupName());

                if (invalidName()) {
                    return toast.error("Backup name already exists");
                }
            }}
        >
            <TextField name="name" helperText={invalidName() && "Name already exists"} error={invalidName()} fullWidth size="small" id="filled-basic" label="Backup name" variant="filled" value={backupName()} onChange={(value) => setBackupName(value.target.value)} />

            <FormControlLabel sx={{
                pt: 1
            }}

                control={<Switch checked={onlyData()} onChange={(e, value) => {
                    console.log(value);
                    setOnlyData(value)}} />}
                label="Only backup app data"
            />
            <RunButton text="Create backup" type="submit" />
        </form>
    </CustomModal>)
}