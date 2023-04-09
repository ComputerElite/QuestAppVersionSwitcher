import { Title, Link, Meta } from '@solidjs/meta';
import { config } from '../store';
import { backupList } from '../state/backups';
import { showChangeGameModal } from '../modals/ChangeGameModal';
import { For, createEffect, createSignal, on } from 'solid-js';
import { Box, Button, IconButton, List, ListItem, Typography } from '@suid/material';
import { IBackup } from '../api/backups';
import { FaSolidWindowRestore } from 'solid-icons/fa';
import { FiTrash } from 'solid-icons/fi'
const [selectedBackup, setSelectedBackup] = createSignal<IBackup | null>(null);
import { showConfirmModal } from '../modals/ConfirmModal';
// import UploadRounded from '@suid/icons-material/UploadRounded';
import PageLayout from '../Layouts/PageLayout';
import RunButton from '../components/Buttons/RunButton';
import { PlusIcon, RestoreIcon } from '../assets/Icons';


export default function BackupPage() {


    // Select first backup if there are any backups and remove selection if there are no backups
    createEffect(on(backupList, (backup) => {
        if (backup == null || backup.backups == null || backup.backups.length == 0) {
            return setSelectedBackup(null);
        }
        if (selectedBackup() == null) {
            setSelectedBackup(backup.backups[0]);
        }
    }))

    async function onDeleteClick(backup: IBackup) {
        console.log("delete");
        let confirm = await showConfirmModal({
            title: "Delete backup",
            message: `Are you sure you want to delete the backup "${backup.backupName}"?`,
            okText: "Delete",
            cancelText: "Cancel",
        })

        if (confirm) {
            console.log("delete confirmed");
        };
    }

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
                    <RunButton text='Create a backup' icon={<PlusIcon />} onClick={() => { }} />
                    <Box sx={{
                        display: "flex",
                        gap: 2,
                        alignItems: "center",
                    }}>
                        <span style={{
                            "font-family": "Roboto",
                            "font-style": "normal",
                            "font-weight": "400",
                            "font-size": "12px",
                            "line-height": "14px",
                            "display": "flex",
                            "align-items": "center",
                            "text-align": "center",
                            "background-clip": "text",
                            // "text-fill-color": "transparent"
                        }} class="text-accent" >
                            Used: {backupList()?.backupsSizeString ?? "?"} / 64.00 GB

                        </span>
                        <RunButton text='Delete all' onClick={() => { }} style={"width: 80px"} />
                    </Box>


                </Box>


                {/* <RunButton text='Backup' icon={<PlusIcon />} onClick={() => { }} />
                    <RunButton text='Backup' icon={<UploadRounded />} disabled onClick={() => { }} />
                    <RunButton text='Error' icon={<PlayArrowRounded />} variant='error' onClick={() => { }} />
                    <RunButton text='Backup' icon={<PlayArrowRounded />} variant='info' onClick={() => { }} />
                    <RunButton text='Backup' icon={<PlayArrowRounded />} variant='success' onClick={() => { }} />
                    <RunButton text='Backup' variant='success' icon={<FirePatch />} onClick={() => { }} />
                    <RunButton text='Backup' icon={<PlayArrowRounded />} variant='success' onClick={() => { }} />
                    <RunButton icon={<PlayArrowRounded />} variant='success' onClick={() => { }} /> */}

                <List sx={{
                    width: '100%',
                    overflowX: 'auto',
                }}>
                    <For each={backupList()?.backups}>
                        {(backup) => (
                            <BackupItem backup={backup} onDeleteClick={onDeleteClick} />
                        )}
                    </For>
                </List>
            </div>
        </PageLayout>

    )
}

interface BackupItemProps {
    backup: IBackup;
    onDeleteClick?: (backup: IBackup) => void;
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
                    }}  class="text-accent">
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
                <IconButton color='info' onClick={() => props.onDeleteClick != null && props?.onDeleteClick(props.backup)}>
                    <RestoreIcon />
                </IconButton>
                <IconButton color='error' onClick={() => props.onDeleteClick != null && props?.onDeleteClick(props.backup)}>
                    <FiTrash />
                </IconButton>
            </Box>
        </ListItem>
    )
}
