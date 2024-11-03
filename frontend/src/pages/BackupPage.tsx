import { Title } from '@solidjs/meta';
import { config } from '../store';
import { backupList, refetchBackups } from '../state/backups';
import { For, createEffect, createSignal, on, onMount } from 'solid-js';
import { Box, IconButton, List, ListItem, Typography } from '@suid/material';
import { IBackup, deleteBackup } from '../api/backups';
import { FiTrash } from 'solid-icons/fi'
const [selectedBackup, setSelectedBackup] = createSignal<IBackup | undefined>(undefined);
import { showConfirmModal } from '../modals/ConfirmModal';
import PageLayout from '../Layouts/PageLayout';
import RunButton from '../components/Buttons/RunButton';
import { PlusIcon, RestoreIcon } from '../assets/Icons';
import toast from 'solid-toast';
import CreateBackupModal from '../modals/CreateBackupModal';
import RestoreBackupModal from '../modals/RestoreBackupModal';


export default function BackupPage() {
    const [createBackupOpen, setCreateBackupOpen] = createSignal(false);
    const [restoreBackupOpen, setRestoreBackupOpen] = createSignal(false);

    // Select first backup if there are any backups and remove selection if there are no backups
    createEffect(on(backupList, (backup) => {
        if (backup == null || backup.backups == null || backup.backups.length == 0) {
            return setSelectedBackup(undefined);
        }
        if (selectedBackup() == null) {
            setSelectedBackup(backup.backups[0]);
        }
    }))

    async function onRestoreClick(backup: IBackup) {
        setSelectedBackup(backup);
        setRestoreBackupOpen(true);
    }

    async function onDeleteClick(backup: IBackup) {
        let confirm = await showConfirmModal({
            title: "Delete backup",
            message: `Are you sure you want to delete the backup "${backup.backupName}"?`,
            okText: "Delete",
            cancelText: "Cancel",
        })

        if (confirm) {
            let result = await deleteBackup(config()!.currentApp, backup.backupName);
            if (result) {
                toast.success("Backup deleted");
                refetchBackups();
            }
        };
    }

    onMount(() => {
        refetchBackups();
    });

    return (
        <PageLayout>
            <div class='contentItem'>
                <Title>Backup</Title>

                <Box sx={{
                    display: "flex",
                    width: "100%",
                    gap: 1,
                    flexWrap: "wrap",
                    justifyContent: "space-between",
                    marginBottom: 2,
                }}>
                    <RunButton text='Create a backup' icon={<PlusIcon />} onClick={() => { setCreateBackupOpen(true) }} hideTextOnMobile />
                    <Box class="flex gap-3 items-center">
                        <span style={{
                            "font-family": "Roboto",
                            "font-style": "normal",
                            "font-weight": "400",
                            "font-size": "12px",
                            "line-height": "14px",
                            "display": "flex",
                            "align-items": "center",
                            "text-align": "center",
                        }} class="text-accent" >
                            Used: {backupList()?.backupsSizeString ?? "0"} / 64.00 GB

                        </span>
                        <RunButton text='Delete all' onClick={() => { }} style={"width: 80px"} />
                    </Box>
                </Box>

                <List sx={{
                    width: '100%',
                    overflowX: 'auto',
                }}>
                    <For each={backupList()?.backups}>
                        {(backup) => (
                            <BackupItem backup={backup} onDeleteClick={onDeleteClick} onRestoreClick={() => {
                                onRestoreClick(backup);
                            }} />
                        )}
                    </For>
                </List>
            </div>
            <CreateBackupModal open={createBackupOpen()} onClose={() => setCreateBackupOpen(false)} />
            <RestoreBackupModal open={restoreBackupOpen()} onClose={() => {
                setRestoreBackupOpen(false);
                setSelectedBackup(undefined);
            }} selectedBackup={selectedBackup()} />
        </PageLayout>

    )
}

interface BackupItemProps {
    backup: IBackup;
    onDeleteClick?: (backup: IBackup) => void;
    onRestoreClick?: (backup: IBackup) => void;
}

/**
 * Removes the underscore long version from the game version
 * @param backupName 
 * @returns 
 */
function removeUnderscoreVersion(backupName?: string) {
    if (backupName == null) {
        return backupName;
    }
    let split = backupName.split("_");
    if (split.length >= 1) {
        split.pop();
    }
    else {
        return backupName;
    }
    return split[0];
}

function BackupItem(props: BackupItemProps) {
    return (
        <ListItem sx={{
            cursor: "pointer",
            display: "flex",
            borderRadius: "6px",
            flexDirection: "row",
            alignItems: "flex-start",
            padding: "15px 20px",
            backgroundColor: selectedBackup()?.backupName == props.backup.backupName ? "#333" : "#222",
            justifyContent: "space-between",
            background: "#111827",
            "&:hover": {
                background: "#1F2937"
            },
        }}
            onClick={() => setSelectedBackup(props.backup)}
        >
            <Box>
                <div style={{
                    "font-family": 'Roboto',
                    "font-style": 'normal',
                    "font-weight": 400,
                    "font-size": '16px',
                    "line-height": '19px',
                    /* identical to box height */
                    color: '#F9FAFB',
                }} >
                    <span >
                        {props.backup.backupName}
                    </span>
                    <span style={{
                        "font-size": '12px',
                        "margin-left": "5px",
                    }} class="text-accent">
                        {removeUnderscoreVersion(props?.backup?.gameVersion)}
                    </span>
                </div>

                <Typography component={"div"} fontSize={"10px"} style={{
                    "margin-top": "6px",
                }} class="text-accent" >
                    {props.backup.backupSizeString}
                </Typography>

            </Box>

            <Box sx={{
                display: "flex",
                gap: 2,
                alignItems: "right",
            }}>
                <IconButton color='info' onClick={() => props.onRestoreClick != null && props?.onRestoreClick(props.backup)}>
                    <RestoreIcon />
                </IconButton>
                <IconButton color='error' onClick={() => props.onDeleteClick != null && props?.onDeleteClick(props.backup)}>
                    <FiTrash />
                </IconButton>
            </Box>
        </ListItem>
    )
}
